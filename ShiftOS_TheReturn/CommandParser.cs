﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShiftOS.Engine {
    public static class CurrentCommandParser {
        public static CommandParser parser;
    }

    public class CommandParser {
        public IList<CommandFormat> parts = new List<CommandFormat>();

        public void AddPart(CommandFormat part) {
            parts.Add(part);
        }

        public void ImportPart(IList<CommandFormat> parts) {
            this.parts = parts;
        }

        public string Save() {
            JArray data = new JArray();
            foreach(CommandFormat part in parts) {
                CFValue val = new CFValue(part);
                JObject obj = new JObject();
                obj["type"] = new JValue(val.type);
                obj["text"] = new JValue(val.text);
                data.Add(obj);
            }

            return data.ToString();
        }

        public static CommandParser Load(string val) {
            CommandParser parser = new CommandParser();
            JArray data = JArray.Parse(val);

            IList<CFValue> values = data.Select(obj => new CFValue (
                (string)obj["type"],
                (string)obj["text"]
            )).ToList();

            foreach(CFValue value in values) {
                parser.AddPart(value.GetCommandFormat());
            }

            return parser;
        }

        public KeyValuePair<KeyValuePair<string, string>, Dictionary<string, string>> ParseCommand(string cdd) {
            string command = "";
            string ns = "";
            Dictionary<string, string> arguments = new Dictionary<string, string>();

            string text = cdd;
            int position = 0;

            int commandPos;
            int firstValuePos = -1;
            int lastValuePos = -1;

            string syntaxError = "";

            for (int ii = 0; ii < parts.Count; ii++) {
                CommandFormat part = parts[ii];
                if (part is CommandFormatMarker) {
                    if (part is CommandFormatCommand) {
                        commandPos = ii;
                    } else if (part is CommandFormatValue) {
                        if (firstValuePos > -1)
                            lastValuePos = ii;
                        else
                            firstValuePos = ii;
                    }
                }
            }

            int i = 0;
            string currentArgument = "";
            int help = -1;

            while (position < text.Length) {

                if (i >= parts.Count) {
                    position = text.Length;
                    command = "+FALSE+";
                    i = 0;
                }

                CommandFormat part = parts[i];
                string res = part.CheckValidity(text.Substring(position));

                // ok so:

                // example
                // COMMAND text[ --] ARGUMENT VALUE text[ --] ARGUMENT VALUE
                // COMMAND text[{] ARGUMENT text[=] VALUE text[, ] ARGUMENT text[=] VALUE text[}]

                if (part is CommandFormatMarker) {
                    if (part is CommandFormatNamespace) {
                        ns = res;
                        help = -1;
                    }else if (part is CommandFormatCommand) {
                        command = res;
                        help = -1;
                    } else if (part is CommandFormatArgument) {
                        currentArgument = res;
                        help = -1;
                    } else if (part is CommandFormatValue) {
                        arguments[currentArgument] = string.Join("", res.Split('"'));

                        if (i == firstValuePos)
                            help = lastValuePos;
                        if (i == lastValuePos)
                            help = firstValuePos;
                    }
                }

                if (res == "+FALSE+") {
                    if (help > -1) {
                        i = help;
                        if (i >= parts.Count) {
                            position = text.Length;
                            command = "+FALSE+";
                        }
                    } else {
                        position = text.Length;
                        syntaxError = "Syntax Error";
                        command = "+FALSE+";
                    }
                    help = -1;
                } else {
                    position += res.Length;
                }

                i++;
            }

            if (command == "+FALSE+") {
                //lblExampleCommand.Text = "Syntax Error";
                return new KeyValuePair<KeyValuePair<string, string>, Dictionary<string, string>>();
            } else {
                /*string argvs = "{";

                foreach (KeyValuePair<string, string> entry in arguments) {
                    argvs += entry.Key + "=" + entry.Value + ", ";
                }

                argvs += "}";

                lblExampleCommand.Text = command + argvs;*/
                return new KeyValuePair<KeyValuePair<string, string>, Dictionary<string, string>> (new KeyValuePair<string, string>(ns, command), arguments);
            }
        }
    }

    public class CFValue {
        public string type { get; set; }
        public string text { get; set; }

        public CFValue(string type, string text) {
            this.type = type;
            this.text = text;
        }

        public CFValue(CommandFormat format) {
            type = "";
            text = "";
            if(format is CommandFormatText) {
                text = ((CommandFormatText) format).str;
                if(format is CommandFormatOptionalText) {
                    type = "optionalText";
                }else if (format is CommandFormatRegex) {
                    type = "regexText";
                }else {
                    type = "text";
                }
            }else if (format is CommandFormatMarker) {
                if (format is CommandFormatNamespace) {
                    type = "namespace";
                } else if (format is CommandFormatCommand) {
                    type = "command";
                } else if (format is CommandFormatArgument) {
                    type = "argument";
                } else if (format is CommandFormatValue) {
                    type = "value";
                }
            }
        }

        public CommandFormat GetCommandFormat() { // TODO update with better code
            switch (type) {
                case "text":
                    return new CommandFormatText(text);
                case "optionalText":
                    return new CommandFormatOptionalText(text);
                case "regexText":
                    return new CommandFormatRegex(text);
                case "namespace":
                    return new CommandFormatNamespace();
                case "command":
                    return new CommandFormatCommand();
                case "argument":
                    return new CommandFormatArgument();
                case "value":
                    return new CommandFormatValue();
                case "color":
                    throw new NotImplementedException(); // fix this (make it not a notimplementedexception)
            }
            return new CommandFormatMarker();
        }
    }


    public interface CommandFormat {
        string CheckValidity(string check);
        Control Draw();
    }
    public class CommandFormatText : CommandFormat {
        public string str = "";
        TextBox textBox;

        public CommandFormatText() {

        }

        public CommandFormatText(string str) {
            this.str = str;
        }

        public virtual string CheckValidity(string check) {
            return check.StartsWith(str) ? str : "+FALSE+";
        }

        public Control Draw() {
            textBox = new TextBox();
            textBox.TextChanged += new EventHandler(TextChanged);
            textBox.Location = new Point(0, 0);
            textBox.Text = str;

            return textBox;
        }

        void TextChanged(object sender, EventArgs e) {
            str = textBox.Text;
        }
    }

    public class CommandFormatOptionalText : CommandFormatText {
        public CommandFormatOptionalText() : base() {
        }
        public CommandFormatOptionalText(string str) : base(str) {
        }

        public override string CheckValidity(string check) {
            return check.StartsWith(str) ? str : "";
        }
    }

    public class CommandFormatRegex : CommandFormatText {
        public CommandFormatRegex() : base() {
        }
        public CommandFormatRegex(string str) : base(str) {
        }

        public override string CheckValidity(string check) {
            Match match = (new Regex("^" + str)).Match(check);
            return match.Success ? match.Value : "+FALSE+";
        }
    }

    public class CommandFormatMarker : CommandFormat {
        protected string str;
        Button button;

        public CommandFormatMarker() {
        }

        public virtual string CheckValidity(string check) {
            string res = string.Empty;
            string alphanumeric = "QWERTYUIOPASDFGHJKLZXCVBNMqwertyuiopasdfghjklzxcvbnm"; // not using regex for performance reasons

            foreach (char c in check) {
                if (alphanumeric.IndexOf(c) > -1) {
                    res += c;
                } else {
                    break;
                }
            }

            return res;
        }

        public virtual Control Draw() {
            button = new Button();
            button.Location = new Point(0, 0);
            button.Text = "Marker";

            return button;
        }
    }

    public class CommandFormatCommand : CommandFormatMarker {
        public override Control Draw() {
            Button draw = (Button)base.Draw();
            draw.Text = "Command";
            return draw;
        }
    }

    public class CommandFormatNamespace : CommandFormatMarker {
        public override Control Draw() {
            Button draw = (Button)base.Draw();
            draw.Text = "Namespace";
            return draw;
        }
    }

    public class CommandFormatArgument : CommandFormatMarker {
        public override Control Draw() {
            Button draw = (Button)base.Draw();
            draw.Text = "Argument";
            return draw;
        }
    }

    public class CommandFormatValue : CommandFormatMarker {
        public override string CheckValidity(string cd) {
            string res = string.Empty;
            var check = "";
            bool done = false;

            if (cd.StartsWith("\"")) {
                check = cd.Substring(1);

                foreach (char c in check) {
                    if (c != '"') {
                        res += c;
                    } else {
                        done = true;
                        res = "\"" + res + "\"";
                        break;
                    }
                }
            } else{
                res = base.CheckValidity(cd);
                done = true;
            }
            return done ? res : "+FALSE+";
        }

        public override Control Draw() {
            Button draw = (Button)base.Draw();
            draw.Text = "\"Value\"";
            return draw;
        }
    }
}
