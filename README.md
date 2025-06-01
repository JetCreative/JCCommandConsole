# JCCommandConsole
An in-game console command system for Unity

Versions 1.1 Now includes predictive text!
Version 2.0 *** To update to v2.0, delete the existing command console scripts and then import. The code was completely rewritten and will be incompatible with v1.***
- Cache commands through editor window or new CommandCache scriptable object inspector to generate the cache before runtime.
- "call" command is now used to indicate methods and delegate commands
- "select" now used gameobject currently selected in the scene as the target for instance methods.
- Improved predictive text.


Requires:
1. Unity
2. TextMesh Pro

How to Use:

1. Import package into your project.
2. Add JCCommandConsolePro and JCCommandConsoleProUI components to an empty gameobject in your scene.
3. Set up UI references in JCCOmmandConsoleProUI component. A basic ui is included but the console will work with custom UIs.
4. Add a [Command] attribute to any public, private property, field, method, or delegate. Static elements are supported as well.

![alt text](https://github.com/JetCreative/JCCommandConsole/blob/main/Images/HowToLabelMethods.png)

5. Demo scene has an component with example static, public, and private properties, fields, delegates, and methods.
4. Click in the inputfield in the bottom of the Console UI to input your command. Command names are automaticaly converted to lower case for the calling in the console purposes.
5. The first word can optionally be a target identifer, the following word will be command and the final word is the command name. Parameters and values follow the command.
6. You can submit commands via "Enter" in the demo, by calling SubmitCommand method in the ConsoleUI class, or click on the "Enter" button to submit the command.
7. The outcome of the command is returned in to the output field. It will show the return value of your commanded method as well as any error.

![alt-text](https://github.com/JetCreative/JCCommandConsole/blob/main/Images/Console.png)

The available target commands are:
1. "@" - find gameobject with the following name. The name is case senstive.
2. "@@" - find all gameobjects with the follow name.
3. "#" - find gameobjects with the follow tag name.
4. "##" - find all gameobjects with the follow tag.
5. "select" - target the currently selected gameobject in the scene heirarchy. (Only available in the editor)

The available console commands are:
1. "get" - return the current value of the property or field.
2. "set" set the value of the property or field.
3. "call" - invoke the method or delegate.

This is 100% me messing around and not wanting to pay $40 for a console asset, so no promises on continued support or updates.

That being said, let me know if you have any suggestions!

Jet Creative

