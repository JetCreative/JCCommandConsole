using JetCreative.Console;
using UnityEngine;

namespace Demo.Demo_Scripts
{
    /// <summary>
    /// Component with example commands callable by JCCommandConsole.
    /// </summary>
    public class ExampleCommands: MonoBehaviour
    {
        [Command] public void TestCommand()
        {
            Debug.Log("Test Command");
        }
        
        [Command] public static void TestStaticCommand()
        {
            Debug.Log("Test Static Command");
        }
        
        [Command] public void TestCommandWithParams(string param1, int param2)
        {
            Debug.Log($"Test Command with params: {param1}, {param2}");
        }
        
        [Command] public int TestCommandWithReturn(int a, int b)
        {
            return a + b;
        }
        
        [Command] public float SetThisField;
        
        [Command] public int SetThisProperty { get; set; }
    }
}