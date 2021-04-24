using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace WarThreads
{
    struct Point
    {
        public int X { get; set; }

        public int Y { get; set; }
    }

    class Program
    {
        private const int CONSOLE_WIDTH = 80, CONSOLE_HEIGHT = 25;
        private const int MAX_MISS = 30;
        private const int LEFT_SIDE = 0, RIGHT_SIDE = CONSOLE_WIDTH;
        private const int FORWARD_DIRECTION = 1, REVERSE_DIRECTION = -1;
        private const int START_GUN_X = CONSOLE_WIDTH / 2, START_GUN_Y = CONSOLE_HEIGHT - 1;
        private const int HIT_CHECK_SLEEP_TIME = 40;
        private const int MAX_TRIES_TO_HIT = 15;
        private const int FREEZETIME = 15000;
        private const int MAX_RANDOM_VALUE = 100;
        private const int SPAWN_GAP = 1000;
        private const int SPAWN_LINE_RANGE = 10;

        private static int miss = 0, hit = 0;

        private const char GUN_CHAR = '|', BULLET_CHAR = '*', WHITESPACE = ' ';

        private static readonly char[] BAD_GAY_CHAR = new char[] { '-', '\\', '|', '/' };

        private static Random random = new Random();

        private static Mutex screenLock = new Mutex();

        private static EventWaitHandle startEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

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
        /// <returns> Если враг виден на экране - true, иначе - false. </returns>
        private static bool IsBadGuyVisible(int x, int dir) => dir == FORWARD_DIRECTION && x != RIGHT_SIDE || dir == REVERSE_DIRECTION && x != LEFT_SIDE;

        /// <summary>
        /// Проверка на попадание по врага.
        /// </summary>
        /// <param name="x"> Позиция столбца, в котором находится враг. </param>
        /// <param name="y"> Позиция строки, в которой находится враг. </param>
        /// <returns> Если во врага попали - true, иначе - false. </returns>
        private static bool IsHitted(int x, int y)
        {
            // на каждой итерации в цикле проверяем попадание по врагу
            for (int i = 0; i < MAX_TRIES_TO_HIT; i++)
            {
                // ожидание
                Thread.Sleep(HIT_CHECK_SLEEP_TIME);
                // если во врага попали, то возвращаем true
                if (GetAt(x, y) == BULLET_CHAR) return true;
            }
            // возвращаем false (во врага не попали)
            return false;
        }

        public static char GetAt(int x, int y)
        {
            return '?';
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
        private static void BadGuy(object y)
        {
            Point position = new Point()
            {
                Y = (int)y
            };

            // позиция столбца, в которой находится враг
            // напрвление движения врага
            int direction;

            // если позиция строки, где появляется враг, является четным числом
            if (position.Y % 2 == 0)
            {
                // то враг появляется с левой стороны экрана
                position.X = LEFT_SIDE;
                // и двигается вправо
                direction = FORWARD_DIRECTION;
            }
            else
            {
                // иначе враг появляется с правой стороны экрана
                position.X = RIGHT_SIDE;
                // и двигается влево
                direction = REVERSE_DIRECTION;
            }

            // пока противник находится в пределах экрана
            while(IsBadGuyVisible(position.X, direction))
            {
                // анимация движения врага
                WriteAt(position.X, position.Y, BAD_GAY_CHAR[position.X % 4]);

                // проверка попадания по врагу 
                bool hitted = IsHitted(position.X, position.Y);

                // удаляем кадр анимации врага с экрана
                WriteAt(position.X, position.Y, WHITESPACE);

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
                position.X += direction;
            }
        }

        private static bool Spawn() => random.Next(MAX_RANDOM_VALUE) < (hit + miss) / 25 + 20;

        private static void BadGuys()
        {
            startEvent.WaitOne(FREEZETIME);

            while (true)
            {
                if (Spawn())
                {
                    Thread badGuyThread = new Thread(BadGuy);
                    badGuyThread.Start(random.Next(SPAWN_LINE_RANGE));
                }
                Thread.Sleep(SPAWN_GAP);
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
