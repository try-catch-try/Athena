from mythic_payloadtype_container.MythicCommandBase import *
import json


class SleepArguments(TaskArguments):
    def __init__(self, command_line, **kwargs):
        super().__init__(command_line)
        self.args = {
            "sleep": CommandParameter(
                name="sleep",
                type=ParameterType.String,
                description="How long to sleep in between communications.",
                required=False,
            ),
            "jitter": CommandParameter(
                name="jitter",
                type=ParameterType.String,
                description="The percentage to stagger the sleep by.",
                required=False,
            )
        }

    async def parse_arguments(self):
        if len(self.command_line) > 0:
            if self.command_line[0] == "{":
                self.load_args_from_json_string(self.command_line)
            else:
                self.add_arg("sleep", self.command_line.split()[0])
                self.add_arg("jitter", self.command_line.split()[1])


class SleepCommand(CommandBase):
    cmd = "sleep"
    needs_admin = False
    help_cmd = "sleep [seconds] [jitter]"
    description = "Change the implant's sleep interval."
    version = 2
    is_exit = False
    is_file_browse = False
    is_process_list = False
    is_download_file = False
    is_upload_file = False
    is_remove_file = False
    author = "@checkymander"
    argument_class = SleepArguments
    attackmapping = ["T1029"]

    async def create_tasking(self, task: MythicTask) -> MythicTask:
        return task

    async def process_response(self, response: AgentResponse):
        pass
