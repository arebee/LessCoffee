﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.IO;
using DotSmart.Properties;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace DotSmart
{
    public class LessCssHandler : ScriptHandlerBase, IHttpHandler
    {
        static string _lessJs;
        static string _lessWsf;

        /// <summary>
        /// Initializes a new instance of the LessCssHandler class.
        /// </summary>
        static LessCssHandler()
        {
            _lessWsf = Path.Combine(TempDirectory, "less.wsf");
            _lessJs = Path.Combine(TempDirectory, "less.js");

            ExportResourceIfNewer(_lessWsf, Resources.LessWsf);
            ExportResourceIfNewer(_lessJs, Resources.LessJs);
        }

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/css";

            context.Response.Write("/* Generated by DotSmart LessCoffee on " + DateTime.Now + " - http://dotsmart.net */ \r\n");
            context.Response.Write("/* Using LESS - Leaner CSS v1.1.3 - http://lesscss.org - Copyright (c) 2010, Alexis Sellier */ \r\n\r\n");

            string lessFile = context.Server.MapPath(context.Request.FilePath);

            renderStylesheet(lessFile, context.Response.Output);

            // TODO: consider rendering error info like so:
            //context.Response.Write("body:after{ content: \"A LESS error occurred!\" }");

            // look for "@import" and add those to dependencies also
            var lessFiles = parseImports(lessFile).Concat(new[] { lessFile }).ToArray();

            SetCacheability(context.Response, lessFiles);
        }

        static IEnumerable<string> parseImports(string lessFileName)
        {
            /* These are equivalent:
             * 
             *   @import "lib.less";
             *   @import "lib";
             * 
             * Ignore css though as they are not imported by LESS
             */
            string dir = Path.GetDirectoryName(lessFileName);

            var importRegex = new Regex(@"@import\s+[""'](.*)[""'];");

            return (from line in File.ReadAllLines(lessFileName)
                    let match = importRegex.Match(line)
                    let file = match.Groups[1].Value
                    where match.Success
                      && !file.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
                    select Path.Combine(dir, Path.ChangeExtension(file, ".less"))
            );
        }

        void renderStylesheet(string scriptFileName, TextWriter output)
        {
            using (var scriptFile = new StreamReader(scriptFileName, Encoding.UTF8))
            using (var stdErr = new StringWriter())
            {
                // So that relative @import paths resolve
                string currentDirectory = Environment.CurrentDirectory;
                Environment.CurrentDirectory = Path.GetDirectoryName(scriptFileName);

                int exitCode = ProcessUtil.Exec("cscript.exe", "//U //nologo \"" + _lessWsf + "\" -", scriptFile, output, stdErr, Encoding.Unicode);
                if (exitCode != 0)
                {
                    output.WriteLine("/* Error in " + Path.GetFileName(scriptFileName).JsEncode() + ": "
                        + stdErr.ToString().Trim() + " */");
                }
                Environment.CurrentDirectory = currentDirectory;
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }

    }
}