using NSwag;
using NSwag.CodeGeneration.CSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NightscoutClient.CodeGeneration
{
    public static class ClientGenerator
    {
        public static async Task Run(string nightscoutBaseUri, string filename)
        {
            var uri = Prep(nightscoutBaseUri);
            var document = await OpenApiDocument.FromUrlAsync(uri);

            var settings = new CSharpClientGeneratorSettings
            {
                ClassName = "Client",
                CSharpGeneratorSettings =
                {
                    Namespace = "Nightscout.API"
                }
            };

            var generator = new CSharpClientGenerator(document, settings);
            var code = generator.GenerateFile();

            File.WriteAllText(filename, code);
        }

        private static string Prep(string nightscoutBaseUri)
        {
            // enforce the correct json definition
            return new UriBuilder(nightscoutBaseUri)
            {
                Path = "swagger.json"
            }.ToString();
        }
    }
}
