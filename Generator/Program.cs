using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Generator
{
    class Program
    {
        static void Main(string[] args)
        {
            var template = @"@inherits TemplateBase
Hello @Value World";
            
            var engine = RazorProjectEngine.Create(
                RazorConfiguration.Default,
                RazorProjectFileSystem.Create(@"."),
                b => b.SetNamespace("Generator"));

            var document = RazorSourceDocument.Create(template, "MyFile");
            var codeDocument = engine.Process(document, null, new List<RazorSourceDocument>(), new List<TagHelperDescriptor>());
            var razorCSharpDocument = codeDocument.GetCSharpDocument();

            var source = razorCSharpDocument.GeneratedCode;
            Console.WriteLine(source);
            
            var compilation = CSharpCompilation.Create("MyCompilation", 
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(
                    MetadataReference.CreateFromFile(typeof(TemplateBase).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(AppDomain.CurrentDomain.GetAssemblies().Single(mr => mr.GetName().Name == "System.Runtime").Location)
                )
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(source));

            var memoryStream = new MemoryStream();
            var result = compilation.Emit(memoryStream);
            if (result.Success)
            {
                memoryStream.Position = 0;

                var assembly = Assembly.Load(memoryStream.ToArray());
                var type = assembly.GetType("Generator.Template");
                var obj = (TemplateBase) Activator.CreateInstance(type);
                obj.Value = "fubar";
                obj.ExecuteAsync().Wait();
                Console.WriteLine("Result: " + obj.Result);
            }
            else
            {
                foreach(var d in result.Diagnostics)
                    Console.WriteLine(d.GetMessage());
            }
        }
    }
}