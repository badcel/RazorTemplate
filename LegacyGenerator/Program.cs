using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CSharp;

namespace Generator
{
    class Program
    {
        static void Main(string[] args)
        {
            var language = new CSharpRazorCodeLanguage();
            var host = new RazorEngineHost(language) {
                DefaultBaseClass = "TemplateBase",
                DefaultClassName = "MyTemplate",
                DefaultNamespace = "Generator",
            };

            RazorTemplateEngine engine = new RazorTemplateEngine(host);

            var sr = new StringReader("hello @Value world");
            var razorResult = engine.GenerateCode(sr);

            var builder = new StringBuilder();
            var writer = new StringWriter(builder);
            
            var provider = new CSharpCodeProvider();
            provider.GenerateCodeFromCompileUnit(razorResult.GeneratedCode, writer, new CodeGeneratorOptions());
            var source = builder.ToString();
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
                var type = assembly.GetType("Generator.MyTemplate");
                var obj = (TemplateBase) Activator.CreateInstance(type);
                obj.Value = "fubar";
                obj.Execute();
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