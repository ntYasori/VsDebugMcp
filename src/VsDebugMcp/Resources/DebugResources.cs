using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Reflection;

namespace VsDebugMcp.Resources;

[McpServerResourceType]
public sealed class DebugResources
{
    private static string ReadEmbeddedResource(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"VsDebugMcp.Resources.Content.{fileName}";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return $"Resource '{fileName}' not found.";
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [McpServerResource(Name = "debug_instructions", UriTemplate = "debug://instructions", MimeType = "text/markdown"),
     Description("General debugging instructions and workflow guide for using the Visual Studio debugger through Claude Code.")]
    public static string GetDebugInstructions()
    {
        return ReadEmbeddedResource("debug_instructions.md");
    }

    [McpServerResource(Name = "troubleshoot_csharp", UriTemplate = "debug://troubleshoot/csharp", MimeType = "text/markdown"),
     Description("C# specific debugging tips and troubleshooting guide.")]
    public static string GetCSharpTroubleshooting()
    {
        return ReadEmbeddedResource("troubleshooting_csharp.md");
    }

    [McpServerResource(Name = "troubleshoot_cpp", UriTemplate = "debug://troubleshoot/cpp", MimeType = "text/markdown"),
     Description("C++ specific debugging tips and troubleshooting guide.")]
    public static string GetCppTroubleshooting()
    {
        return ReadEmbeddedResource("troubleshooting_cpp.md");
    }

    [McpServerResource(Name = "troubleshoot_fsharp", UriTemplate = "debug://troubleshoot/fsharp", MimeType = "text/markdown"),
     Description("F# specific debugging tips and troubleshooting guide.")]
    public static string GetFSharpTroubleshooting()
    {
        return ReadEmbeddedResource("troubleshooting_fsharp.md");
    }

    [McpServerResource(Name = "troubleshoot_vb", UriTemplate = "debug://troubleshoot/vb", MimeType = "text/markdown"),
     Description("Visual Basic .NET specific debugging tips and troubleshooting guide.")]
    public static string GetVbTroubleshooting()
    {
        return ReadEmbeddedResource("troubleshooting_vb.md");
    }
}
