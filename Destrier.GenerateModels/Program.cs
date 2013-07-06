using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Destrier;

namespace Destrier.GenerateModels
{
    class Program
    {
        public static void Main(string[] args)
        {
            if (args == null || args.Length < 2)
            {
                Usage();
            }

            Console.WriteLine("Destrier Model Generator");

            var connectionString = args[0];
            var model_namespace = args[1];

            String outputDirectory = null;
            if(args.Length > 2)
                outputDirectory = args[2];

            foreach (var table in Schema.GetTables(connectionString))
            {
                Console.WriteLine("Generating: " + table.TableName);

                var fileName = String.Format("{0}.cs", table.TableName);
                
                if(!String.IsNullOrEmpty(outputDirectory))
                {
                    var directoryPath = System.IO.Path.GetFullPath(outputDirectory);

                    if(!Directory.Exists(directoryPath))
                        Directory.CreateDirectory(directoryPath);

                    fileName = System.IO.Path.Combine(directoryPath, fileName);
                }

                using (var file = File.Open(fileName, FileMode.OpenOrCreate))
                {
                    using (var sw = new StreamWriter(file))
                    {
                        var preamble = "using System;\nusing System.Collections.Generic;\nusing System.Linq;\nusing System.Text;\nusing Destrier;\n\nnamespace " + model_namespace + "\n{";
                        sw.Write(preamble);
                        sw.Write("\n\t[Table(\""+  table.TableName + "\")]\n");
                        sw.Write("\tpublic class " + table.TableName + "\n\t{\n");

                        foreach (var column in Schema.GetColumnsForTable(table.TableName, connectionString: connectionString))
                        {
                            if (column.IsPrimaryKey)
                                sw.Write(String.Format("\t\t[Column(IsPrimaryKey=true, IsAutoIdentity={0})]\n", column.IsAutoIdentity.ToString().ToLower()));
                            else
                            {
                                if (!column.CanBeNull)
                                    sw.Write(String.Format("\t\t[Column(CanBeNull=false)]\n"));
                                else
                                    sw.Write(String.Format("\t\t[Column]\n"));
                            }
                            sw.Write("\t\tpublic " + Schema.GetClrType(column.SqlSysType).Name.ToTitleCase() + " " + column.Name.ToTitleCase() + " { get; set; }\n");   
                        }

                        sw.Write("\t}\n");
                        sw.Write("}");
                    }
                }
            }
        }

        public static void Usage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("destrier.modelgenerate.exe connection_string namespace [output directory]");
            Environment.Exit(1);
        }
    }
}
