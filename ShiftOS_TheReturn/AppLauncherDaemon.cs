/*
 * MIT License
 * 
 * Copyright (c) 2017 Michael VanOverbeek and ShiftOS devs
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using ShiftOS.Objects.ShiftFS;

namespace ShiftOS.Engine
{
    public static class AppLauncherDaemon
    {
        public static bool Contains(this AssemblyName[] asms, string name)
        {
            foreach(var asm in asms)
            {
                if (asm.FullName.Contains(name))
                    return true;
            }
            return false;
        }

        public static List<LauncherItem> Available()
        {
            List<LauncherItem> win = new List<LauncherItem>();
            
            foreach (var asmExec in System.IO.Directory.GetFiles(Environment.CurrentDirectory))
            {
                if (asmExec.EndsWith(".dll") | asmExec.EndsWith(".exe"))
                {
                    try
                    {
                        var asm = Assembly.LoadFrom(asmExec);

                        if (asm.GetReferencedAssemblies().Contains("ShiftOS.Engine") || asm.FullName.Contains("ShiftOS.Engine"))
                        {
                            foreach (var type in asm.GetTypes())
                            {
                                if (type.GetInterfaces().Contains(typeof(IShiftOSWindow)))
                                {
                                    foreach (var attr in type.GetCustomAttributes(false))
                                    {
                                        bool isAllowed = true;
                                        if(attr is MultiplayerOnlyAttribute)
                                        {
                                            if(KernelWatchdog.MudConnected == false)
                                            {
                                                isAllowed = false;

                                            }
                                        }
                                        if (isAllowed == true)
                                        {
                                            if (attr is LauncherAttribute)
                                            {
                                                var launch = attr as LauncherAttribute;
                                                if (launch.UpgradeInstalled)
                                                {
                                                    win.Add(new LauncherItem { DisplayData = launch, LaunchType = type });
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {

                    }
                }
            }

            foreach(var file in Utils.GetFiles(Paths.GetPath("applauncher")))
            {
                if (file.EndsWith(".al"))
                {
                    var item = JsonConvert.DeserializeObject<LuaLauncherItem>(Utils.ReadAllText(file));
                    win.Add(item);
                }
            }
            return win;
        }

    }

    public class LauncherItem
    {
        public LauncherAttribute DisplayData { get; internal set; }
        public Type LaunchType { get; internal set; }

    }

    public class LuaLauncherItem : LauncherItem
    {
        public LuaLauncherItem(string file)
        {
            LaunchPath = file;
        }

        public string LaunchPath { get; private set; }
    }
}
