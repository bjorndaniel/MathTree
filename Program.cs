using System;
using Ooui;

namespace MathTree 
{
    class Program 
    {
        static void Main (string[] args) 
        {
            Xamarin.Forms.Forms.Init ();
            new Addition().Publish();
            new Addition(true).PublishEasy();
            UI.Port = 666;
            // UI.Present("/addition");
            Console.ReadLine ();
        }
    }
}