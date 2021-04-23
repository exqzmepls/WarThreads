using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
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
        private const int HIT_CHECK_SLEEP_TIME = 40;

        private static int miss = 0, hit = 0;

        private const char GUN_CHAR = '|', BULLET_CHAR = '*', WHITESPACE = ' ';

        private static readonly char[] BAD_GAY_CHAR = new char[] { '-', '\\', '|', '/' };


        private static Mutex screenLock = new Mutex();

        private static void SetConsoleSize()
        {
            Console.WindowWidth = CONSOLE_WIDTH;
            Console.BufferWidth = CONSOLE_WIDTH;
            Console.WindowHeight = CONSOLE_HEIGHT;
            Console.BufferHeight = CONSOLE_HEIGHT;
        }

        /// <summary>
        /// Демонстрация счета 
        /// </summary>
        private static void ShowScore()
        {
            Console.Title = $"Война потоков - Попаданий:{hit}, Промахов:{miss}";
        }

        private static bool IsGameOver()
        {
            if (miss >= MAX_MISS)
            {
                // game over
            }
            return false;
        }

        /// <summary>
        /// Проверка видимости врага на экране.
        /// </summary>
        /// <param name="x"> Позиция столбца, в котором находится враг. </param>
        /// <param name="dir"> Направление движения врага. </param>
        /// <returns> Если враг виден на экране - True, иначе - False. </returns>
        private static bool IsBadGuyVisible(int x, int dir) => dir == FORWARD_DIRECTION && x != RIGHT_SIDE || dir == REVERSE_DIRECTION && x != LEFT_SIDE;

        private static bool IsHitted(int x, int y)
        {
            for (int i = 0; i < 15; i++) // 15?
            {
                Thread.Sleep(HIT_CHECK_SLEEP_TIME);
                if ('?' == BULLET_CHAR) // getat()
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Вывод на консоль символа в указанной позиции.
        /// </summary>
        /// <param name="x"> Позиция столбца курсора. </param>
        /// <param name="y"> Позиция строки курсора. </param>
        /// <param name="c"> Символ для записи. </param>
        private static void WriteAt(int x, int y, char c)
        {
            // блокировка вывода на консоль через мьютекс
            screenLock.WaitOne();

            // вывод символа на консоль
            Console.SetCursorPosition(x, y);
            Console.Write(c);

            // освобождаем мьютекс
            screenLock.ReleaseMutex();
        }

        /// <summary>
        /// Поток противника.
        /// </summary>
        /// <param name="y"> Позиция строки, в которой появляется враг. </param>
        private static void BadGuy(int y)
        {
            // позиция столбца, в которой находится враг
            int x;
            // напрвление движения врага
            int direction;

            // если позиция строки, где появляется враг, является четным числом
            if (y % 2 == 0)
            {
                // то враг появляется с левой стороны экрана
                x = LEFT_SIDE;
                // и двигается вправо
                direction = FORWARD_DIRECTION;
            }
            else
            {
                // иначе враг появляется с правой стороны экрана
                x = RIGHT_SIDE;
                // и двигается влево
                direction = REVERSE_DIRECTION;
            }

            // пока противник находится в пределах экрана
            while(IsBadGuyVisible(x, direction))
            {
                // анимация движения врага
                WriteAt(x, y, BAD_GAY_CHAR[x % 4]);

                // проверка попадания по врагу 
                bool hitted = IsHitted(x, y);

                // удаляем кадр анимации врага с экрана
                WriteAt(x, y, WHITESPACE);

                // если во врага попали, то
                if (hitted)
                {
                    // звуковой сигнал о попадании
                    SystemSounds.Beep.Play();
                    // увеличиваем количество попаданий на 1
                    Interlocked.Increment(ref hit);
                    // обновляем счет игры
                    ShowScore();
                    // выходим из функции (завершаем поток)
                    return;
                }

                // перемещаем врага в следующую позицию
                x += direction;
            }
        }

        private static void Bullet()
        {

        }

        static void Main(string[] args)
        {
            Console.CursorVisible = false;
            SetConsoleSize();

            // .....

            ShowScore();

            int gunX = START_GUN_X, gunY = START_GUN_Y;

            Console.ReadKey(); // tmp
        }
    }
}
