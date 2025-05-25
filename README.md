# JCCommandConsole
An in-game console command system for Unity

Requires:
1. Unity
2. TextMesh Pro

How to Use:

1. Import package into your project.
2. Add a [Command] attribute to any public method. You can optionally specify the in console name using an optional parameter of the attribute.
---
Example: <br />
<br />
[Command("mymethod")]<br />
public void MethodName <br />
{ <br />
// do something// <br />
}
----

5. There is currently no built in way to open the console but you can open/close however you wish by calling JCCommandConsole.Instance.EnableConsole().
4. Click in the inputfield in the bottom of the Console UI to input your command. Method names are automaticaly converted to lower case for the calling in the console purposes.
5. Currently, you must click on the "Enter" button to submit the command.
6. The outcome of the command is returned in to the output field. It will show the return value of your commanded method as well as any error.

This is 100% me messing around and not wanting to pay $40 for a console asset, so no promises on continued support or updates.

That being said, let me know if you have any suggestions!

Jet Creative

