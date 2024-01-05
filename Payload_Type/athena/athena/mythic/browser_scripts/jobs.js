function(task, responses){
    if(task.status.includes("error")){
        const combined = responses.reduce( (prev, cur) => {
            return prev + cur;
        }, "");
        return {'plaintext': combined};
    }else if(task.completed){
        if(responses.length > 0){
            try{
                let output_table = [];
                var jsonObject = JSON.parse(responses[0]);
                for (var key in jsonObject) {
                    output_table.push({
                        "Guid":{"plaintext": key},
                        "Command": {"plaintext": jsonObject[key]},
                    })
                }
                    return {
                        "table": [
                            {
                                "headers": [
                                    {"plaintext": "Guid", "type": "string", "width": 300},
                                    {"plaintext": "Command", "type": "string"},
                                ],
                                "rows": output_table,
                                "title": "Environmental Variables"
                            }
                        ]
                    }
                }catch(error){
                    console.log(error);
                    const combined = responses.reduce( (prev, cur) => {
                        return prev + cur;
                    }, "");
                    return {'plaintext': combined};
            }
        }else{
            return {"plaintext": "No output from command"};
        }
    }else{
        return {"plaintext": "No data to display..."};
    }
}