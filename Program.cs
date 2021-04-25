using System;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WarThreads
{
    class Program
    {
        private const int
            CONSOLE_WIDTH = 80, CONSOLE_HEIGHT = 25, // ширина и высота консоли
            MAX_MISS = 30, // максимально допустимое количество промахов
            LEFT_SIDE_BORDER = 0, RIGHT_SIDE_BORDER = CONSOLE_WIDTH - 1, // граница левой и правой сторны консоли
            FORWARD_DIRECTION = 1, REVERSE_DIRECTION = -1, // прямое и обратное направления движения врагов
            START_GUN_X = CONSOLE_WIDTH / 2, START_GUN_Y = CONSOLE_HEIGHT - 1, // начальные координаты пушки
            HIT_CHECK_SLEEP_TIME = 40, // задержка между проверками на попадание
            MAX_TRIES_TO_HIT = 15, // количество проверок на попадание
            FREEZETIME = 15000, // время до начала игры
            MAX_RANDOM_VALUE = 100, // максимальный шанс на "не создание" врага
            SPAWN_GAP = 1000, // промежуток между попытками создать врага
            SPAWN_LINE_RANGE = 10, // граница области, в которой могут появляться враги
            MAX_BULLETS = 3, // максимальное количество пуль, которое может одновременно находится на экране
            BULLET_SEMAPHORE_WAITTIME = 0, // время ожидания на семафоре
            BULLET_FRAME_LIFESPAN = 100, // время на один кадр анимации пули
            GAP_BETWEEN_BULLETS = 100, // задержка между двумя подряд выпущенными пулями
            DEFAULT_SPAWN_CHANCE = 20, // стандартный шанс на создание врага
            NUMBER_OF_BAD_GUYS_TO_COMPLICATE = 25, // количество созданных врагов, после которого повышается шанс на создание врага
            STD_OUTPUT_HANDLE = -11; // активный буфер экрана консоли

        private static readonly char[] BAD_GAY_CHAR = new char[] { '-', '\\', '|', '/' }; // массив с символами для анимации врага
        private const char 
            GUN_CHAR = '|', // символ пушки
            BULLET_CHAR = '*', // символ пули
            WHITESPACE = ' '; // пробел

        // Количество промахов и попаданий
        private static int miss = 0, hit = 0;

        // Переменаня для генерации "случайных" чисел
        private static Random random = new Random();

        // Дескриптор вывода консоли
        private static IntPtr consoleOut = GetStdHandle(STD_OUTPUT_HANDLE);

        // Мьютекс для блокирования доступа к выводу на экран
        private static Mutex screenLock = new Mutex();

        // Событие начала игры
        private static EventWaitHandle startEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

        // Семафор для констроля количества пуль, одновременно находящихся на экране
        private static Semaphore bulletSemaphore = new Semaphore(MAX_BULLETS, MAX_BULLETS);

        // Объект, который захватывает критическая секция при завершении игры
        private static object gameOver = new object();

        // Главный поток
        private static Thread mainThread = Thread.CurrentThread;

        /// Определяем управляемый метод, имеющий точно такую же сигнатуру, что и неуправляемый.
        /// <summary>
        /// Извлекает дескриптор для указанного стандартного устройства.
        /// </summary>
        /// <param name="nStdHandle"> Стандартное устройство. </param>
        /// <returns> Дескриптор для указанного устройства или перенаправленный дескриптор. </returns>
        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        /// Определяем управляемый метод, имеющий точно такую же сигнатуру, что и неуправляемый.
        /// <summary>
        /// Копирует ряд символов из последовательных ячеек буфера экрана консоли, начиная с указанного расположения.
        /// </summary>
        /// <param name="hConsoleOutput"> Дескриптор буфера экрана консоли. </param>
        /// <param name="lpCharacter"> Указатель на буфер, который получает символы, считанные из буфера экрана консоли. </param>
        /// <param name="nLength"> Число ячеек символов буфера экрана, из которых производится чтение. </param>
        /// <param name="dwReadCoord"> Координаты первой ячейки в буфере экрана консоли, из которой производится чтение. </param>
        /// <param name="lpNumberOfCharsRead"> Указатель на переменную, которая получает количество фактически считанных символов. </param>
        /// <returns> Строка считанных символов. </returns>
        [DllImport("kernel32", SetLastError = true)]
        private static extern bool ReadConsoleOutputCharacter(IntPtr hConsoleOutput, [Out] StringBuilder lpCharacter, uint nLength, COORD dwReadCoord, out uint lpNumberOfCharsRead);

        // Определяем структуру для параметра "dwReadCoord" функции "ReadConsoleOutputCharacter"
        [StructLayout(LayoutKind.Sequential)]
        private struct COORD
        {
            public short X;
            public short Y;
        }

        /// <summary>
        /// Устанавливаем размер окна и буфера консоли.
        /// </summary>
        private static void SetConsoleSize()
        {
            Console.WindowWidth = CONSOLE_WIDTH + 1;
            Console.BufferWidth = CONSOLE_WIDTH + 1;
            Console.WindowHeight = CONSOLE_HEIGHT;
            Console.BufferHeight = CONSOLE_HEIGHT;
        }

        /// <summary>
        /// Демонстрация счета 
        /// </summary>
        private static void ShowScore() => Console.Title = $"Война потоков - Попаданий:{hit}, Промахов:{miss}";

        /// <summary>
        /// Проверка условия завершения игры.
        /// </summary>
        /// <returns> Если количество промахов не меньше допустимого - true, иначе - false. </returns>
        private static bool IsGameOver() => miss >= MAX_MISS;

        /// <summary>
        /// Условие для создания нового врага.
        /// </summary>
        /// <returns> Если необходимо создать нового врага - true, иначе - false. </returns>
        private static bool Spawn() => random.Next(MAX_RANDOM_VALUE) < (hit + miss) / NUMBER_OF_BAD_GUYS_TO_COMPLICATE + DEFAULT_SPAWN_CHANCE;

        /// <summary>
        /// Проверка видимости врага на экране.
        /// </summary>
        /// <param name="x"> Позиция столбца, в котором находится враг. </param>
        /// <param name="dir"> Направление движения врага. </param>
        /// <returns> Если враг виден на экране - true, иначе - false. </returns>
        private static bool IsBadGuyVisible(int x, int dir) => dir == FORWARD_DIRECTION && x != RIGHT_SIDE_BORDER || dir == REVERSE_DIRECTION && x != LEFT_SIDE_BORDER;

        /// <summary>
        /// Проверка на видимость пули.
        /// </summary>
        /// <param name="y"> Позиция строки, в которой назодится пуля. </param>
        /// <returns> Если пулю видно - true, иначе - false. </returns>
        private static bool IsBulletVisible(int y) => y > 0;

        /// <summary>
        /// Проверка на попадание по врагу.
        /// </summary>
        /// <param name="badGuyPosition"> Позиция, в котором находится враг. </param>
        /// <returns> Если во врага попали - true, иначе - false. </returns>
        private static bool IsHitted(Point badGuyPosition)
        {
            // на каждой итерации в цикле проверяем попадание по врагу
            for (int i = 0; i < MAX_TRIES_TO_HIT; i++)
            {
                // ожидание
                Thread.Sleep(HIT_CHECK_SLEEP_TIME);
                // если во врага попали, то возвращаем true
                if (GetAt(badGuyPosition) == BULLET_CHAR) return true;
            }
            // возвращаем false (во врага не попали)
            return false;
        }

        /// <summary>
        /// Выход из игры.
        /// </summary>
        [Obsolete]
        private static void Exit()
        {
            // Входим в критическую секцию
            Monitor.Enter(gameOver);
            // Приостанавливаем работу главного потока
            mainThread.Suspend();
            // Выводим сообщение о завершении игры
            MessageBox.Show("Игра окончена!", "Война потоков", MessageBoxButtons.OK);
            // Завершаем работу программы
            Environment.Exit(0);
        }

        /// <summary>
        /// Получение символа из заданной позиции консоли.
        /// </summary>
        /// <param name="point"> Позиция, из которой нужно получить символ. </param>
        /// <returns> Символ в указанной позиции. </returns>
        public static char GetAt(Point point)
        {
            // строка, в которую помещаются считанные символы
            StringBuilder c = new StringBuilder(1);

            // количество считываемых символов
            uint length = 1;

            // блокировка доступа к консоли
            screenLock.WaitOne();

            // считываем символы из консоли
            ReadConsoleOutputCharacter(consoleOut, c, length, new COORD() { X = (short)point.X, Y = (short)point.Y }, out uint numberOfCharactersRead);

            // освобождаем мьютекс (разрешаем доступ к консоли)
            screenLock.ReleaseMutex();

            // возвращаем считанный символ
            return c.ToString()[(int)numberOfCharactersRead - 1];
        }

        /// <summary>
        /// Вывод на консоль символа в указанной позиции.
        /// </summary>
        /// <param name="point"> Позиция курсора. </param>
        /// <param name="c"> Символ для записи. </param>
        private static void WriteAt(Point point, char c)
        {
            // блокировка доступа к консоли
            screenLock.WaitOne();

            // вывод символа на консоль
            Console.SetCursorPosition(point.X, point.Y);
            Console.Write(c);

            // освобождаем мьютекс (разрешаем доступ к консоли)
            screenLock.ReleaseMutex();
        }

        /// <summary>
        /// Поток противника.
        /// </summary>
        /// <param name="y"> Позиция строки, в которой появляется враг. </param>
        [Obsolete]
        private static void BadGuy(object y)
        {
            // позиция врага
            Point position = new Point() { Y = (int)y };

            // напрвление движения врага
            int direction;

            // если позиция строки, где появляется враг, является четным числом
            if (position.Y % 2 == 0)
            {
                // то враг появляется с левой стороны экрана
                position.X = LEFT_SIDE_BORDER;
                // и двигается вправо
                direction = FORWARD_DIRECTION;
            }
            else
            {
                // иначе враг появляется с правой стороны экрана
                position.X = RIGHT_SIDE_BORDER;
                // и двигается влево
                direction = REVERSE_DIRECTION;
            }

            // пока противник находится в пределах экрана
            while(IsBadGuyVisible(position.X, direction))
            {
                // анимация движения врага
                WriteAt(position, BAD_GAY_CHAR[position.X % 4]);

                // проверка попадания по врагу 
                bool hitted = IsHitted(position);

                // удаляем кадр анимации врага с экрана
                WriteAt(position, WHITESPACE);

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

            // звуковой сигнал о промахе
            SystemSounds.Hand.Play();
            // враг убежал, увеличиваем количество промахов на 1
            Interlocked.Increment(ref miss);
            // обновляем счет игры
            ShowScore();
            // если совершено достаточное количество промахов, то завершаем игру
            if (IsGameOver()) Exit();
        }


        /// <summary>
        /// Поток создания врагов.
        /// </summary>
        [Obsolete]
        private static void BadGuys()
        {
            // ждем сигнала к началу игры
            startEvent.WaitOne(FREEZETIME);

            // цикл создания врагов
            while (true)
            {
                // создаем врага, если условие выполнилось
                if (Spawn())
                {
                    // создаем новый поток врага
                    Thread badGuyThread = new Thread(BadGuy);
                    // запускаем поток
                    badGuyThread.Start(random.Next(SPAWN_LINE_RANGE));
                }
                // ожидаем некоторый промежуток времени между созданием врагов
                Thread.Sleep(SPAWN_GAP);
            }
        }

        /// <summary>
        /// Поток пули.
        /// </summary>
        /// <param name="position"> Начальная позиция пули на консоли. </param>
        private static void Bullet(object position)
        {
            // приводим параметр к структуре
            Point _position = (Point)position;

            // если в даннной позиции уже есть пуля, то выстрела не происходит
            if (GetAt(_position) == BULLET_CHAR) return;

            // проверям семафор
            if (!bulletSemaphore.WaitOne(BULLET_SEMAPHORE_WAITTIME)) return; // если нет свободных мест, то выстрела не происходит

            // пока пуля видна на экране
            while (IsBulletVisible(--_position.Y))
            {
                // отображаем пулю
                WriteAt(_position, BULLET_CHAR);
                // ждем промежуток времени между кадрами анимации
                Thread.Sleep(BULLET_FRAME_LIFESPAN);
                // стераем пулю
                WriteAt(_position, WHITESPACE);
            }

            // освобождаем семафор
            bulletSemaphore.Release();
        }

        [Obsolete]
        static void Main(string[] args)
        {
            //mainThread = Thread.CurrentThread;

            // настройка консоли
            Console.CursorVisible = false;
            SetConsoleSize();

            // вывод счета
            ShowScore();

            // позиция пушки
            Point gunPosition = new Point() { X = START_GUN_X, Y = START_GUN_Y };

            // инициализируем поток для создания врагов
            Thread badGuysThread = new Thread(BadGuys);
            // запускаем поток
            badGuysThread.Start();

            // в цикле обрабатыеваем нажатие клавиш и перемещаем пушку
            while (true)
            {
                // рисуем пушку
                WriteAt(gunPosition, GUN_CHAR);

                //Console.SetCursorPosition(0, 0);

                // считываем клавишу
                switch (Console.ReadKey(true).Key)
                {
                    // если пробел, то выстрел
                    case ConsoleKey.Spacebar:
                        // создаем поток выстрела
                        Thread bulletThread = new Thread(Bullet);
                        // запусткаем поток
                        bulletThread.Start(gunPosition);
                        // ждем между выстрелами
                        Thread.Sleep(GAP_BETWEEN_BULLETS);
                        break;

                    // если стрелка влево, то перемещаем пушку влево
                    case ConsoleKey.LeftArrow:
                        // начинаем игру
                        startEvent.Set();
                        // если пушка не в левом краю
                        if (gunPosition.X > LEFT_SIDE_BORDER)
                        {
                            // стриаем пушку
                            WriteAt(gunPosition, WHITESPACE);
                            // перемещаем пушку влево
                            gunPosition.X--;
                        }
                        break;

                    // если стрелка вправо, то перемещаем пушку вправо
                    case ConsoleKey.RightArrow:
                        // начинаем игру
                        startEvent.Set();
                        // если пушка не в правом краю
                        if (gunPosition.X < RIGHT_SIDE_BORDER)
                        {
                            // стриаем пушку
                            WriteAt(gunPosition, WHITESPACE);
                            // перемещаем пушку вправо
                            gunPosition.X++;
                        }
                        break;
                }
            }
        }
    }

    struct Point
    {
        public int X { get; set; }

        public int Y { get; set; }
    }
}
