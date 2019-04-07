using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace ADUsers
{
    class Option
    {
        public enum Action
        {
            None = 0,
            Help,
            List,
            Add,
            Remove
        }
        public enum LogLevel
        {
            None = 0,
            Error,
            Information,
            Warning,
            Verbose
        }
        public Action ADUserAction { get; set; }
        public string ldapDomain { get; set; }
        public string ldapPath { get; set; }

        public string userName { get; set; }
        public string password { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string description { get; set; }
        public LogLevel ConsoleLevel { get; set; }
        public LogLevel TraceLevel { get; set; }
        public string TraceFile { get; set; }
        public int TraceSize { get; set; }
        private string ErrorMessage = string.Empty;
        public string GetErrorMessage()
        {
            return ErrorMessage;
        }
        public Option()
        {
            this.firstName = string.Empty;
            this.lastName = string.Empty;
            this.userName = string.Empty;
            this.password = string.Empty;
            this.ldapDomain = string.Empty;
            this.ldapPath = string.Empty;
            this.TraceFile = string.Empty;
            this.TraceSize = 524280;
            this.TraceLevel = LogLevel.Information;
            this.ConsoleLevel = LogLevel.Information;
        }
        private string Version = "1.0.0.1";
        private string InformationMessage = "ADUser:\r\n" + "Version: {0} \r\n" + "Syntax:\r\n" +
    "ADUser --list --domain <domain-for instance: testwvd.pw> --ldappath <LDAP Path-for instance: CN=Users,DC=testwvd,DC=pw>\r\n" +
    "ADUser --add  --domain <domain-for instance: testwvd.pw> --ldappath <LDAP Path-for instance: CN=Users,DC=testwvd,DC=pw>\r\n" +
    "                 [--firstname <firstname> ]\r\n" +
    "                 [--lastname <lastname> ]\r\n" +
    "                 [--description <description> ]\r\n" +
    "                 [--username <username> ]\r\n" +
    "                 [--password <password> ]\r\n" +
    "                 [--tracefile <path> --tracesize <size in bytes> --tracelevel <none|error|information|warning|verbose>]\r\n" +
    "                 [--consolelevel <none|error|information|warning|verbose>]\r\n" +
    "ADUser --remove --domain <domain>  --ldappath <LDAP Path>  \r\n" +
    "                 [--username <username> ]\r\n" +
    "                 [--tracefile <path> --tracesize <size in bytes> --tracelevel <none|error|information|warning|verbose>]\r\n" +
    "                 [--consolelevel <none|error|information|warning|verbose>]\r\n" +
    "ADUser --help\r\n";
        public void PrintHelp()
        {
            LogInformation(string.Format(InformationMessage, Version));
        }
        void LogMessage(LogLevel level, string Message)
        {
            string Text = string.Empty;
            if ((level <= TraceLevel) && (!string.IsNullOrEmpty(this.TraceFile)))
            {
                Text = string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + " " + Message + "\r\n";
                LogTrace(this.TraceFile, this.TraceSize, Text);
            }
            if (level <= ConsoleLevel)
            {
                if (string.IsNullOrEmpty(Text))
                    Text = string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + " " + Message + "\r\n";
                Console.Write(Text);
            }
        }
        public void LogVerbose(string Message)
        {
            LogMessage(LogLevel.Verbose, Message);
        }
        public void LogInformation(string Message)
        {
            LogMessage(LogLevel.Information, Message);
        }
        public void LogWarning(string Message)
        {
            LogMessage(LogLevel.Warning, Message);
        }
        public void LogError(string Message)
        {
            LogMessage(LogLevel.Error, Message);
        }
        static public char GetChar(byte b)
        {
            if ((b >= 32) && (b < 127))
                return (char)b;
            return '.';
        }
        static public string DumpHex(byte[] data)
        {
            string result = string.Empty;
            string resultHex = " ";
            string resultASCII = " ";
            int Len = ((data.Length % 16 == 0) ? (data.Length / 16) : (data.Length / 16) + 1) * 16;
            for (int i = 0; i < Len; i++)
            {
                if (i < data.Length)
                {
                    resultASCII += string.Format("{0}", GetChar(data[i]));
                    resultHex += string.Format("{0:X2} ", data[i]);
                }
                else
                {
                    resultASCII += " ";
                    resultHex += "   ";
                }
                if (i % 16 == 15)
                {
                    result += string.Format("{0:X8} ", i - 15) + resultHex + resultASCII + "\r\n";
                    resultHex = " ";
                    resultASCII = " ";
                }
            }
            return result;
        }
        public ulong LogTrace(string fullPath, long Tracefile, string Message)
        {
            ulong retVal = 0;

            try
            {
                lock (this)
                {
                    FileStream fs = new FileStream(fullPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    if (fs != null)
                    {
                        long pos = fs.Seek(0, SeekOrigin.End);
                        byte[] data = UTF8Encoding.UTF8.GetBytes(Message);
                        if (data != null)
                        {
                            if (pos + data.Length > Tracefile)
                                fs.SetLength(0);
                            fs.Write(data, 0, data.Length);
                            retVal = (ulong)data.Length;
                        }
                        fs.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while append in file:" + fullPath + " Exception: " + ex.Message);
            }
            return retVal;
        }
        static public LogLevel GetLogLevel(string text)
        {
            LogLevel level = LogLevel.None;
            switch (text.ToLower())
            {
                case "none":
                    level = LogLevel.None;
                    break;
                case "information":
                    level = LogLevel.Information;
                    break;
                case "error":
                    level = LogLevel.Error;
                    break;
                case "warning":
                    level = LogLevel.Warning;
                    break;
                case "verbose":
                    level = LogLevel.Verbose;
                    break;
                default:
                    break;
            }
            return level;
        }
        public static Option ParseCommandLine(string[] args)
        {
            Option option = new Option();

            if (option == null) 
            {
                return null;
            }
            try
            {

                option.TraceFile = "ADUser.log";
                if (args != null)
                {

                    int i = 0;
                    if (args.Length == 0)
                    {
                        option.ErrorMessage = "No parameter in the command line";
                        return option;
                    }
                    while ((i < args.Length) && (string.IsNullOrEmpty(option.ErrorMessage)))
                    {
                        switch (args[i++])
                        {

                            case "--help":
                                option.ADUserAction = Action.Help;
                                break;
                            case "--add":
                                option.ADUserAction = Action.Add;
                                break;
                            case "--list":
                                option.ADUserAction = Action.List;
                                break;
                            case "--remove":
                                option.ADUserAction = Action.Remove;
                                break;
                            case "--firstname":
                                if ((i < args.Length) && (!string.IsNullOrEmpty(args[i])))
                                    option.firstName = args[i++];
                                else
                                    option.ErrorMessage = "first name not set";
                                break;
                            case "--lastname":
                                if ((i < args.Length) && (!string.IsNullOrEmpty(args[i])))
                                    option.lastName = args[i++];
                                else
                                    option.ErrorMessage = "last name not set";
                                break;
                            case "--username":
                                if ((i < args.Length) && (!string.IsNullOrEmpty(args[i])))
                                    option.userName = args[i++];
                                else
                                    option.ErrorMessage = "user name not set";
                                break;
                            case "--password":
                                if ((i < args.Length) && (!string.IsNullOrEmpty(args[i])))
                                    option.password = args[i++];
                                else
                                    option.ErrorMessage = "password not set";
                                break;
                            case "--description":
                                if ((i < args.Length) && (!string.IsNullOrEmpty(args[i])))
                                    option.description = args[i++];
                                else
                                    option.ErrorMessage = "description not set";
                                break;
                            case "--domain":
                                if ((i < args.Length) && (!string.IsNullOrEmpty(args[i])))
                                    option.ldapDomain = args[i++];
                                else
                                    option.ErrorMessage = "domain not set";
                                break;
                            case "--ldappath":
                                if ((i < args.Length) && (!string.IsNullOrEmpty(args[i])))
                                    option.ldapPath = args[i++];
                                else
                                    option.ErrorMessage = "ldap path not set";
                                break;
                            case "--tracefile":
                                if ((i < args.Length) && (!string.IsNullOrEmpty(args[i])))
                                    option.TraceFile = args[i++];
                                else
                                    option.ErrorMessage = "TraceFile not set";
                                break;
                            case "--tracesize":
                                if ((i < args.Length) && (!string.IsNullOrEmpty(args[i])))
                                {
                                    int tracesize = 0;
                                    if (int.TryParse(args[i++], out tracesize))
                                        option.TraceSize = tracesize;
                                    else
                                        option.ErrorMessage = "TraceSize value incorrect";
                                }
                                else
                                    option.ErrorMessage = "TraceSize not set";
                                break;
                            case "--tracelevel":
                                if ((i < args.Length) && (!string.IsNullOrEmpty(args[i])))
                                    option.TraceLevel = GetLogLevel(args[i++]);
                                else
                                    option.ErrorMessage = "TraceLevel not set";
                                break;
                            case "--consolelevel":
                                if ((i < args.Length) && (!string.IsNullOrEmpty(args[i])))
                                    option.ConsoleLevel = GetLogLevel(args[i++]);
                                else
                                    option.ErrorMessage = "ConsoleLevel not set";
                                break;
                            default:
                                option.ErrorMessage = "wrong parameter: " + args[i - 1];
                                return option;
                        }
                    }

                    if (option.ADUserAction == Action.None)
                    {
                        option.ErrorMessage = "No feature in the command line";
                        return option;
                    }
                }
            }
            catch (Exception ex)
            {
                option.ErrorMessage = "Exception while analyzing the options: " + ex.Message;
                return option;
            }

            if (!string.IsNullOrEmpty(option.ErrorMessage))
            {
                return option;
            }
            return CheckOptions(option);
        }
        public static Option CheckOptions(Option option)
        {
            if (option.ADUserAction == Action.Help) 
            {
                return option;
            }
            else if (option.ADUserAction == Action.Add)
            {
                if (
                    (!string.IsNullOrEmpty(option.firstName)) &&
                    (!string.IsNullOrEmpty(option.lastName)) &&
                    (!string.IsNullOrEmpty(option.ldapDomain)) &&
                    (!string.IsNullOrEmpty(option.ldapPath)) &&
                    (!string.IsNullOrEmpty(option.description)) &&
                    (!string.IsNullOrEmpty(option.userName)) &&
                    (!string.IsNullOrEmpty(option.password)) 
                    )
                {

                    return option;
                }
                else
                    option.ErrorMessage = "Missing parameters for Add feature";
            }
            else if (option.ADUserAction == Action.Remove)
            {
                if (
                    (!string.IsNullOrEmpty(option.ldapDomain)) &&
                    (!string.IsNullOrEmpty(option.ldapPath)) &&
                    (!string.IsNullOrEmpty(option.userName)) 
                    )
                {

                    return option;
                }
                else
                    option.ErrorMessage = "Missing parameters for Remove feature";
            }
            else if (option.ADUserAction == Action.List)
            {
                if (
                    (!string.IsNullOrEmpty(option.ldapDomain)) &&
                    (!string.IsNullOrEmpty(option.ldapPath)) 
                    )
                {

                    return option;
                }
                else
                    option.ErrorMessage = "Missing parameters for List feature";
            }
            return null;
        }
    }
}
