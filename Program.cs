using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WarThreads
{
    class Program
    {
        private const int CONSOLE_WIDTH = 80, CONSOLE_HEIGHT = 25;
        private const int MAX_MISS = 30;
        private const int LEFT_SIDE = 0, RIGHT_SIDE = CONSOLE_WIDTH;
        private const int FORWARD_DIRECTION = 1, REVERSE_DIRECTION = -1;
        private const int START_GUN_X = CONSOLE_WIDTH / 2, START_GUN_Y = CONSOLE_HEIGHT - 1;

        private static int _miss = 0, _hit = 0;

        private const char GUN_CHAR = '|', BULLET_CHAR = '*', WHITESPACE = ' ';

        private static readonly char[] _badchar = new char[] { '-', '\\', '|', '/' };

        private static void SetConsoleSize()
        {
            Console.WindowWidth = CONSOLE_WIDTH;
            Console.BufferWidth = CONSOLE_WIDTH;
            Console.WindowHeight = CONSOLE_HEIGHT;
            Console.BufferHeight = CONSOLE_HEIGHT;
        }

        private static void ShowScore()
        {
            Console.Title = $"Война потоков - Попаданий:{_hit}, Промахов:{_miss}";
        }

        private static bool IsGameOver()
        {
            if (_miss >= MAX_MISS)
            {
                // game over
            }
            return false;
        }

        private static bool IsEven(int i)
        {
            return i % 2 == 0;
        }

        private static bool IsBullet(char c)
        {
            return c == BULLET_CHAR;
        }

        private static bool IsBadGuyVisible(int x, int dir)
        {
            return dir == FORWARD_DIRECTION && x != RIGHT_SIDE || dir == REVERSE_DIRECTION && x != LEFT_SIDE;
        }

        private static void BadGuy(int y)
        {
            int x, direction;

            if (IsEven(y))
            {
                x = LEFT_SIDE;
                direction = FORWARD_DIRECTION;
            }
            else
            {
                x = RIGHT_SIDE;
                direction = REVERSE_DIRECTION;
            }

            while(IsBadGuyVisible(x, direction))
            {
                bool hitted = false;

                // writeat()

                for (int i = 0; i < 15; i++)
                {
                    // sleep()
                    if (IsBullet('?')) // getat()
                    {
                        hitted = true;
                        break;
                    }
                }
                // writeat()

                if (hitted)
                {
                    // ...
                    return; // ??????
                }
                x += direction;
            }
        }

        private static void Bullet()
        {

        }

        static void Main(string[] args)
        {
            SetConsoleSize();

            // .....

            ShowScore();

            int gunX = START_GUN_X, gunY = START_GUN_Y;

            Console.ReadKey(); // tmp
        }
    }
}
