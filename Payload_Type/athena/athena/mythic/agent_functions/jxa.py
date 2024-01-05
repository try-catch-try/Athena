from mythic_container.MythicCommandBase import *
import json
from mythic_container.MythicRPC import *

from .athena_utils import message_converter


class JxaArguments(TaskArguments):
    def __init__(self, command_line, **kwargs):
        super().__init__(command_line)
        self.args = [            
            CommandParameter(
                name="code",
                type=ParameterType.String,
                description="code to execute",
            )]

    async def parse_arguments(self):
        if self.command_line[0] == "{":
            self.load_args_from_json_string(self.command_line)
        else:
            self.add_arg("code", self.command_line)


class JxaCommand(CommandBase):
    cmd = "jxa"
    needs_admin = False
    help_cmd = "jxa [code]"
    description = "Execute jxa code"
    version = 1
    author = "@checkymander"
    attackmapping = ["T1082"]
    argument_class = JxaArguments
    attributes = CommandAttributes(
    )
    async def create_go_tasking(self, taskData: PTTaskMessageAllData) -> PTTaskCreateTaskingMessageResponse:
        response = PTTaskCreateTaskingMessageResponse(
            TaskID=taskData.Task.ID,
            Success=True,
        )
        return response

    async def process_response(self, task: PTTaskMessageAllData, response: any) -> PTTaskProcessResponseMessageResponse:
        if "message" in response:
            user_output = response["message"]
            await MythicRPC().execute("create_output", task_id=task.Task.ID, output=message_converter.translateAthenaMessage(user_output))

        resp = PTTaskProcessResponseMessageResponse(TaskID=task.Task.ID, Success=True)
        return resp