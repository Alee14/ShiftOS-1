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
using System.Text;
using System.Threading.Tasks;

namespace ShiftOS.Engine
{
    public static class TutorialManager
    {
        private static ITutorial _tut = null;

        public static void RegisterTutorial(ITutorial tut)
        {
            IsInTutorial = false;
            _tut = tut;
            _tut.OnComplete += (o, a) =>
            {
                SaveSystem.CurrentSave.StoryPosition = 2;
                IsInTutorial = false;
            };
        }

        public static bool IsInTutorial
        {
            get; private set;
        }

        public static int Progress
        {
            get
            {
                return _tut.TutorialProgress;
            }
        }

        public static void StartTutorial()
        {
            IsInTutorial = true;
            _tut.Start();
        }
    }

    public interface ITutorial
    {
        int TutorialProgress { get; set; }
        void Start();
        event EventHandler OnComplete;
    }

    public class TutorialLockAttribute : Attribute
    {
        public TutorialLockAttribute(int progress)
        {
            Progress = progress;
        }
        
        public TutorialLockAttribute() : this(0)
        {
            
        }

        public int Progress { get; private set; }
    }
}
