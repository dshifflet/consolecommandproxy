# consolecommandproxy
.Net Core 2.2 http host takes a file via post and runs a shell command on it then returns the output of the command.

Set the command up in:
appsettings.json

  "AllowedHosts": "*",
  "OutputExtension" : ".zip",
  "Command" : "zip",
  "Arguments" : "\"{1}\" \"{0}\"",
  "TimeoutInMs" : 5000

Arguments {0} is the input, {1} is the output.  The command to run in the shell is ZIP.  These look reversed because of mac.

  To change the URLs and ports edit the hosting.json.
  {
    "server.urls": "http://localhost:5100;http://localhost:5101;http://*:5102"
  }