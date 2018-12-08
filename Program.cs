using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PowershellToCMDConvert
{
    class Program
    {
        static int Main(string[] args)
        {
            string outFileName = string.Empty;
            string inFileName = string.Empty;
            const string DEFAULT_EXT = "cmd";

            Converter convert = new Converter();

            if (args.Length < 1)
            {
                Console.WriteLine("Usage: powershelltocmd <input> <output>");
                return 1;
            }

            if (args.Length >= 1)
                inFileName = args[0];
            if (args.Length >= 2)
                outFileName = args[1];
            else
                outFileName = convert.ReplaceExt(inFileName, DEFAULT_EXT);

            StreamReader instream;
            StreamWriter outstream;

            instream = new StreamReader(new FileStream(inFileName, FileMode.Open));

            try
            {
                outstream = new StreamWriter(new FileStream(outFileName, FileMode.CreateNew));
            } 
            catch (IOException ex)
            {
                Console.WriteLine("The file already exists. Overwriting");
                File.Delete(outFileName);
                outstream = new StreamWriter(new FileStream(outFileName, FileMode.CreateNew));
            }

            string inpowershell = instream.ReadToEnd();
            string outpowershell = convert.GetPowershellCommandString(inpowershell);
            string cmd = convert.GenerateCMD(inFileName, outpowershell, pauseAtEnd: true);

            foreach (string line in cmd.Split('\n'))
                outstream.WriteLine(line);

            instream.Close();
            outstream.Close();

            Console.WriteLine("Converted to {0}", outFileName);
            return 0;
        }
    }

    class Converter
    {
        public string GenerateCMD(string name, string powershell, string authors = "Author Unknown", string version = "1.0", bool pauseAtEnd = false, bool echoOff = true)
        {
            string result = "";
            if (echoOff)
                result = "@echo off\n";

            result += string.Format(":: {0} {1}\n:: By {2}", name, version, authors); // Append name, version, and authors
            result = string.Format("{0}\n\npowershell -Command \"{1}\"", result, powershell); // Append wrapped Powershell command

            if (pauseAtEnd)
                result += "\npause"; // If pausing add our pause

            return result;
        }

        public string GetPowershellCommandString(string powershell)
        {
            if (string.IsNullOrEmpty(powershell))
                return "";

            string[] lines = powershell.Split('\n');
            string result = "";

            for (int i = 0; i < lines.Length; i++)
            {
                if (isEchoLine(lines[i]))
                    lines[i] = EchoLine(lines[i]); // Convert any only quote lines to echo lines
                else if (isAssignment(lines[i]))
                    lines[i] = AssignLine(lines[i]);
            }

            result = lines[0].Trim(); // Append first line with no semicolo before it
            for (int i = 1; i < lines.Length; i++)
            {
                if (! string.IsNullOrWhiteSpace(lines[i])) // Do not include whitespace lines
                    result = string.Format("{0}; {1}", result, lines[i].Trim());
            }

            return result;
        }

        public string ReplaceExt(string filename, string ext)
        {
            int period = filename.LastIndexOf('.');

            if (period != -1)
                filename = filename.Substring(0, period);

            return string.Format("{0}.{1}", filename, ext);
        }

        bool isAssignment(string line)
        {
            char firstactual = GetFirstCharacter(line);
            int equals = line.IndexOf('=');

            if (equals != -1 && firstactual == '$')
                return true;
            return false;
        }

        char GetFirstCharacter(string line)
        {
            char firstactual = ' ';
            for (int i = 0; i < line.Length; i++)
            {
                if (!char.IsWhiteSpace(line[i]))
                {
                    firstactual = line[i]; // Get the first nonwhitespace character of the line to find out if its a quote
                    break;
                }
            }
            return firstactual; // else whitespace if nothing
        }

        bool isEchoLine(string line)
        {
            char firstactual = GetFirstCharacter(line);
            int containsEquals = line.IndexOf('=');

            if (firstactual == '\"' || (firstactual == '$' && containsEquals == -1)) // If the first symbol is a ", or it's a $ and this is not an assignment, it should be an echo
                return true;
            return false;
        }

        string AssignLine(string msg)
        {
            string name = "", value = "";

            int dollar = msg.IndexOf('$');
            int equals = msg.IndexOf('=');

            name = msg.Substring(dollar + 1, equals - 2).Trim();
            value = msg.Substring(equals + 1).Trim();

            return string.Format("Set-Variable -Name '{0}' -Value '{1}'", name, value);
        }

        string EchoLine(string msg)
        {
            if (msg.IndexOf('"') == msg.LastIndexOf('"') - 1) // If the quotes are right next to each other, usually meaning newline
                return "Write-Host";
            else
                return string.Format("Write-Host {0}", msg.Trim());
        }
    }
}
