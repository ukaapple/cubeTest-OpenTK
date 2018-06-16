using System;

namespace cubeTest_OpenTK
{
    class Program
    {
        static void Main(string[] args)
        {
            new CubeInsWindow(100, 100, 100).Run(60);
        }
    }
}
