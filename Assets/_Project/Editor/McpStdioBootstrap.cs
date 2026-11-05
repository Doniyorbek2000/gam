using UnityEditor;

namespace BilliardGame.Editor
{
    // Pins MCP for Unity to stdio so the bridge listens on its TCP port without the HTTP server;
    // .mcp.json launches the Python side. Runs once per machine, then is a no-op.
    [InitializeOnLoad]
    internal static class McpStdioBootstrap
    {
        private const string UseHttpKey = "MCPForUnity.UseHttpTransport";

        static McpStdioBootstrap()
        {
            if (EditorPrefs.HasKey(UseHttpKey) && !EditorPrefs.GetBool(UseHttpKey)) return;
            EditorPrefs.SetBool(UseHttpKey, false);
            EditorUtility.RequestScriptReload();
        }
    }
}
