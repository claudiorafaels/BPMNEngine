﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;
using System.Text;
using System.Security.Cryptography;

namespace BPMNEngine.Elements.Processes.Scripts
{
    internal abstract class ACompiledScript : AScript
    {
        private const string _NAME_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvzwxyz";
        private const int _NAME_LENGTH = 32;

        private static readonly string[] IMPORTS = new[] { "System", "BPMNEngine", "BPMNEngine.Interfaces", "BPMNEngine.Interfaces.Variables", "System.Linq" };

        protected string ClassName { get; private init; }
        protected string FunctionName { get;private init; }

        private IEnumerable<string> Imports
            => IMPORTS
            .Concat(SubNodes
                .Where(n => n.NodeType==XmlNodeType.Element && n.Name.Equals("using",StringComparison.InvariantCultureIgnoreCase))
                .Select(n => n.InnerText)
            )
            .Distinct();

        private IEnumerable<string> Dlls
            => new string[] { Assembly.GetAssembly(this.GetType()).Location }
            .Concat(SubNodes
                .Where(n => n.NodeType==XmlNodeType.Element && n.Name.Equals("dll", StringComparison.InvariantCultureIgnoreCase))
                .Select(n => n.InnerText)
            );
        
        private Assembly _assembly;

        public ACompiledScript(XmlElement elem, XmlPrefixMap map, AElement parent)
            : base(elem, map, parent)
        {
            ClassName = NextName();
            FunctionName = NextName();
        }

        protected abstract EmitResult Compile(string name, IEnumerable<MetadataReference> references, IEnumerable<string> imports, string code, out byte[] compiled);
        
        private bool CompileAssembly(out string errors)
        {
            errors = null;
            lock (this)
            {
                if (_assembly == null)
                {
                    var references = AppDomain.CurrentDomain.GetAssemblies()
                        .Where(ass => GetAssemblyLocation(ass) != null)
                        .Select(ass => MetadataReference.CreateFromFile(GetAssemblyLocation(ass)))
                        .Concat(
                            Dlls
                            .Select(d=> MetadataReference.CreateFromFile(d))
                        );
                    EmitResult res = Compile(NextName(), references, Imports, Code, out byte[] compiled);
                    if (!res.Success)
                    {
                        var error = new StringBuilder();
                        res.Diagnostics.ForEach(diag => error.AppendLine(diag.ToString()));
                        errors = string.Format("Unable to compile script Code.  Errors:{0}", error.ToString());
                        _assembly = null;
                    }
                    else
                        _assembly = Assembly.Load(compiled);
                }
            }
            return errors == null;
        }

        protected static string GetAssemblyLocation(Assembly ass)
        {
            try
            {
                return ass.Location=="" ? null : ass.Location;
            }catch(Exception)
            {
                return null;
            }
        }

        protected override void ScriptInvoke<T>(T variables, out object result)
        {
            Debug("Creating new instance of compiled script class for script element {0}", new object[] { ID });
            object o = _assembly.CreateInstance(ClassName);
            Debug("Accesing method from new instance of compiled script class for script element {0}", new object[] { ID });
            MethodInfo mi = o.GetType().GetMethod(FunctionName);
            object[] args = new object[] { variables };
            Debug("Executing method from new instance of compiled script class for script element {0}", new object[] { ID });
            if (mi.ReturnType==typeof(void))
            {
                mi.Invoke(o, args);
                result=null;
            }
            else
                result = mi.Invoke(o, args);
        }

        protected override bool ScriptIsValid(out IEnumerable<string> err)
        {
            _assembly = null;
            if (!CompileAssembly(out string error))
            {
                err = new string[] { error };
                return false;
            }
            else
                err = null;
            return true;
        }

        private static string NextName()
        {
            var result = new StringBuilder();
            for (var x = 0; x<_NAME_LENGTH; x++)
                result.Append(_NAME_CHARS[RandomNumberGenerator.GetInt32(_NAME_CHARS.Length - 1)]);
            return result.ToString();
        }
    }
}
