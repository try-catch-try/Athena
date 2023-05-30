def translateAthenaMessage(message):
    messages = {
        "0x01" : "Invalid profile specified",
        "0x02" : "Profile updated successfully",
        "0x03" : "Successfully unlinked from Agent",
        "0x04" : "Failed to unlink agent, ID was invalid",
        "0x05" : "Socks Started",
        "0x06" : "Failed to start socks",
        "0x07" : "Socks Stopped",
        "0x08" : "Failed to stop socks",
        "0x09" : "We don't mean no harm, but we truly mean all the disrespect",
        "0x0A" : "Sleep and Jitter updated successfully",
        "0x0B" : "Failed to update sleep and jitter",
        "0x0C" : "Failed to switch context!",
        "0x0D" : "Cancelled job",
        "0x0E" : "Job doesn't exist",
        "0x0F" : "Not implemented yet",
        "0x10" : "Not available in this configuration",
        "0x11" : "Plugin not loaded. Please use the load command to load the plugin!",
        "0x12" : "Mythic sent no data to upload!",
        "0x13" : "No File ID received from server",
        "0x14" : "Established link with pipe.",
        "0x15" : "Failed to establish link with pipe",
        "0x16" : "An error occurred while attempting to access the file",
        "0x17" : "Failed to link, no valid forwarder specified",
        "0x18" : "Invalid target specified",
        "0x19" : "Successfully loaded assembly",
        "0x1A" : "Couldn't get a handle on Std.Out!",
        "0x1B" : "AssemblyLoadContext reset.",
        "0x1C" : "Command already loaded.",
        "0x1D" : "Command loaded!",
        "0x1E" : "Failed to load command, no assignable type.",
        "0x1F" : "Plugins unloaded!",
        "0x20" : "Std.Out is busy with another command!",
        "0x21" : "Not available in this configuration",
        "0x22" : "Token created successfully.",
        "0x23" : "Stopped Farmer.",
        "0x24" : "A file was provided but contained no data",
        "0x25" : "No targets provided",
        "0x26" : "ID not specified!",
        "0x27" : "No Path Specified",
        "0x28" : "Done, check filebrowser for output.",
        "0x29" : "No files returned.",
        "0x2A" : "Please specify a directory to create!",
        "0x2B" : "Please specify both a source and destination for the file!",
        "0x2C" : "Finished, check process browser for output",
        "0x2D" : "No valid session specified",
        "0x2E" : "No valid action specified",
        "0x2F" : "No active sessions",
        "0x30" : "File stream was empty",
        "0x31" : "Failed to connect to host",
        "0x32" : "No client to disconnect from, removing from sessions list",
        "0x33" : "Disconnected",
        "0x34" : "Failed to disconnect",
        "0x35" : "Buffer finished executing.",
        "0x36" : "Switched session.",
        "0x37" : "No active connections. Please use connect to log into a host!",
        "0x38" : "No command specified",
        "0x39" : "Successfully stopped listener",
        "0x40" : "Failed to stop listener",
        "0x41" : "Successfully started listener",
        "0x42" : "Failed to start listener",
    }

    if message in messages:
        return messages[message]

    return message