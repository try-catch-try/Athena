from mythic_container.MythicCommandBase import *  # import the basics
from mythic_container.MythicRPC import *
from mythic_container.logging import *
from .athena_utils.mythicrpc_utilities import *
from .athena_utils.bof_utilities import *
import base64

# create a class that extends TaskArguments class that will supply all the arguments needed for this command
class CoffArguments(TaskArguments):
    def __init__(self, command_line, **kwargs):
        super().__init__(command_line, **kwargs)
        # this is the part where you'd add in your additional tasking parameters
        self.args = [
            CommandParameter(
                name="coffFile",
                type=ParameterType.File,
                description="Upload COFF file to be executed (typically ends in .o)",
                parameter_group_info=[
                    ParameterGroupInfo(
                        required=True,
                        ui_position=0,
                        group_name="Default"
                        ),
                        ParameterGroupInfo(
                        ui_position=0,
                        required=True,
                        group_name="Argument String"
                        ),
                    ],
            ),
            CommandParameter(
                name="functionName",
                type=ParameterType.String,
                description="Name of entry function to execute in COFF",
                default_value="go",
                parameter_group_info=[
                    ParameterGroupInfo(
                        ui_position=1,
                        required=True,
                        group_name="Default"
                        ),
                        ParameterGroupInfo(
                        ui_position=1,
                        required=True,
                        group_name="Argument String"
                        ),
                    ],
            ),
            CommandParameter(
                name="arguments",
                type=ParameterType.String,
                description="Arguments converted to bytes using beacon_compatibility.py",
                default_value="",
                parameter_group_info=[
                    ParameterGroupInfo(
                        ui_position=2,
                        required=True,
                        group_name="Argument String"
                        ),
                    ],
            ),
            CommandParameter(
                name="argument_array",
                type=ParameterType.TypedArray,
                choices=["int16", "int32", "string", "wchar", "base64"],
                description="""Arguments to pass to the COFF via the following way:
                -s:123 or int16:123
                -i:123 or int32:123
                -z:hello or string:hello
                -Z:hello or wchar:hello
                -b:SGVsbG9Xb3JsZA== or base64:SGVsbG9Xb3JsZA==""",
                typedarray_parse_function=self.get_arguments,
                default_value=[],
                parameter_group_info=[
                    ParameterGroupInfo(
                        ui_position=2,
                        required=False,
                        group_name="Default"
                        ),
                    ],
            ),
            CommandParameter(
                name="timeout",
                type=ParameterType.String,
                description="Time to wait for the coff file to execute before killing it",
                default_value="30",
                parameter_group_info=[
                    ParameterGroupInfo(
                        ui_position=3,
                        required=False,
                        group_name="Default"
                        ),
                        ParameterGroupInfo(
                        ui_position=3,
                        required=True,
                        group_name="Argument String"
                        ),
                    ],
            ),
        ]

    # you must implement this function so that you can parse out user typed input into your paramters or load your parameters based on some JSON input
    async def parse_arguments(self):
        if len(self.command_line) > 0:
            if self.command_line[0] == "{":
                self.load_args_from_json_string(self.command_line)

    async def get_arguments(self, arguments: PTRPCTypedArrayParseFunctionMessage) -> PTRPCTypedArrayParseFunctionMessageResponse:
        argumentResponse = PTRPCTypedArrayParseFunctionMessageResponse(Success=True)
        argumentSplitArray = []
        for argValue in arguments.InputArray:
            argSplitResult = argValue.split(" ")
            for spaceSplitArg in argSplitResult:
                argumentSplitArray.append(spaceSplitArg)
        coff_arguments = []
        for argument in argumentSplitArray:
            argType,value = argument.split(":",1)
            value = value.strip("\'").strip("\"")
            if argType == "":
                pass
            elif argType == "int16" or argType == "-s":
                coff_arguments.append(["int16",int(value)])
            elif argType == "int32" or argType == "-i":
                coff_arguments.append(["int32",int(value)])
            elif argType == "string" or argType == "-z":
                coff_arguments.append(["string",value])
            elif argType == "wchar" or argType == "-Z":
                coff_arguments.append(["wchar",value])
            elif argType == "base64" or argType == "-b":
                coff_arguments.append(["base64",value])
            else:
                return PTRPCTypedArrayParseFunctionMessageResponse(Success=False, Error=f"Failed to parse argument: {argument}: Unknown value type.")

        argumentResponse = PTRPCTypedArrayParseFunctionMessageResponse(Success=True, TypedArray=coff_arguments)
        return argumentResponse


# this is information about the command itself
class CoffCommand(CommandBase):
    cmd = "coff"
    needs_admin = False
    help_cmd = "coff"
    description = "Execute a COFF file in process. Leverages the Netitude RunOF project. argumentData can be generated using the beacon_generate.py script found in the TrustedSec COFFLoader GitHub repo. This command is not intended to be used directly, but can be."
    version = 1
    author = "@checkymander & @scottctaylor12"
    argument_class = CoffArguments
    attackmapping = ["T1620"]
    attributes = CommandAttributes(
        load_only=False,
        builtin=False,
        supported_os=[SupportedOS.Windows],
    )

    async def create_go_tasking(self, taskData: PTTaskMessageAllData) -> PTTaskCreateTaskingMessageResponse:
        response = PTTaskCreateTaskingMessageResponse(
            TaskID=taskData.Task.ID,
            Success=True,
        )
        parameter_group = taskData.args.get_parameter_group_name()

        # Retrieve and decode the COFF file
        encoded_file_contents = await get_mythic_file(taskData.args.get_arg("coffFile"))
        decoded_buffer = base64.b64decode(encoded_file_contents)
        original_file_name = await get_mythic_file_name(taskData.args.get_arg("coffFile"))

        # Add arguments for file size and contents
        taskData.args.add_arg(
            "fileSize",
            f"{len(decoded_buffer)}",
            parameter_group_info=[ParameterGroupInfo(group_name=parameter_group, required=True, ui_position=3)],
        )
        taskData.args.add_arg(
            "asm",
            encoded_file_contents,
            parameter_group_info=[ParameterGroupInfo(group_name=parameter_group, required=True, ui_position=3)],
        )

        # Handle argument array if not in "Argument String" group
        if parameter_group != "Argument String":
            taskargs = taskData.args.get_arg("argument_array")
            if not taskargs:
                taskData.args.add_arg(
                    "arguments",
                    "",
                    parameter_group_info=[ParameterGroupInfo(group_name=parameter_group, required=True, ui_position=3)],
                )
            else:
                # Map argument types to corresponding functions
                arg_generators = {
                    "int16": generate16bitInt,
                    "int32": generate32bitInt,
                    "string": generateString,
                    "wchar": generateWString,
                    "base64": generateBinary,
                }

                # Generate and serialize arguments
                serialized_args = []
                for type_array in taskargs:
                    arg_type, value = type_array
                    if arg_type in arg_generators:
                        serialized_args.append(arg_generators[arg_type](value))

                encoded_args = base64.b64encode(SerializeArgs(serialized_args)).decode("utf-8")
                taskData.args.add_arg(
                    "arguments",
                    encoded_args,
                    parameter_group_info=[ParameterGroupInfo(group_name=parameter_group, required=True, ui_position=3)],
                )

            # Remove the argument array after processing
            taskData.args.remove_arg("argument_array")

        # Set display parameters
        response.DisplayParams = "-coffFile {} -functionName {} -timeout {} -arguments {}".format(
            original_file_name,
            taskData.args.get_arg("functionName"),
            taskData.args.get_arg("timeout"),
            taskData.args.get_arg("arguments"),
        )

        return response
    async def process_response(self, task: PTTaskMessageAllData, response: any) -> PTTaskProcessResponseMessageResponse:
        pass
