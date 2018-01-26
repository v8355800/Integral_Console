using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Integral
{
    namespace IO
    {
        public class IntegralIO
        {
            private int _matrixDelay = 3;

            public void Command(string octal)
            {
                Debug.WriteLine("Command: {0}", octal, null);
            }

            public void Value(string octal)
            {
                Debug.WriteLine("Value: {0}", octal, null);
            }

            public void Matrix(string octal)
            {
                Debug.WriteLine("Matrix: {0}", octal, null);
            }

            public ushort K100()
            {
                Debug.WriteLine("K100: ");
                return 0;
            }
            public ushort K101()
            {
                Debug.WriteLine("K101: ");
                return 0;
            }

            public ushort Data() { return 0; }

            public int matrixDelay => _matrixDelay;
        }
    }

    public class TestResult
    {
        double _value;
        bool _brak;
        bool _osc;        // Генерация
        bool _overloadP;  // Перегрузка+
        bool _overloadM;  // Перегрузка-

        public TestResult()
        {
            Clear();
        }

        public void Clear()
        {
            _value     = 0.0;
            _brak      = false;
            _osc       = false;
            _overloadP = false;
            _overloadM = false;
        }
    }

    public class Test
    {
        enum TestType { ttRegular, ttClassification, ttDifferential, ttCharacteristic };

        private IO.IntegralIO _io;
        private string _text;
        private string _number;
        private string[] _codes;
        private TestType _type;
        private TestResult _result;
        //private double _value;

        public override string ToString() => _text;
        public string Number => _number;
        public string[] Codes => _codes;

        public Test(IO.IntegralIO testerIO)
        {
            _io     = testerIO;
            _text   = "";
            _number = "";
            _codes  = new string[0];
            _result = new TestResult();
            //_value  = 0.0;
        }

        public Test(IO.IntegralIO testerIO, string text) : this(testerIO)
        {
            Parse(text);
        }

        public void Clear()
        {
            _text   = "";
            _number = "";
            _codes  = new string[0];
            _result.Clear();
        }

        public int Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            _text = text.Trim();
            _codes = _text.Split('\t');

            // Номер теста
            if (_codes[0].ToUpper()[0] == 'T')
                _number = _codes[0].Substring(1);
            else
                return 0;

            _codes = _codes.Where(w => w != _codes[0]).ToArray();
            foreach (string code in _codes)
            {
                if (code.Length != 4)
                    return 0;

                ushort res;
                if (ushort.TryParse(code, out res))
                {
                    if (res > 7777)
                        return 0;
                }
                else
                    return 0;
            }

            return _codes.Length;
        }

        public bool Execute()
        {
            var index = 0;
            while (index < _codes.Length)
            {
                var hiWord = byte.Parse(_codes[index].Substring(0, 2));
                var loWord = byte.Parse(_codes[index].Substring(2, 2));
                var Word = ushort.Parse(_codes[index]);

                //if (( Word >= 1000 && Word <= 3999) ||
                //    ( Word >= 4700 && Word <= 5099) ||
                //    ( Word >= 5400 && Word <= 7299))
                //    return false;

                switch (hiWord)
                {
                    // 02 - Сброс ранее введенной программы.
                    case 02:
                        Console.WriteLine("\tCommand {0:000}: {1} - Сброс ранее введенной программы", (index), _codes[index]);
                        _io.Command(_codes[index]);
                        index++;
                        break;

                    // 03 - Включение индикации поста оператора и
                    //      отключение измерительных цепей пульта.
                    case 03:
                        Console.WriteLine("\tCommand {0:000}: {1} - Вкл. индикации поста оператора и откл. изм. цепей пульта", (index), _codes[index]);
                        _io.Command(_codes[index]);
                        index++;
                        break;

                    // "7300" - Остановить выполнение программы.
                    case 73:
                        Console.WriteLine("\tCode {0:000}: {1} - Остановка", (index), _codes[index]);
                        MessageBox.Show("Остановка по коду \"7300\"", "Тестер", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                        index++;
                        break;

                    // "7400" - Программирование коммутации в блоке матрицы М201.
                    case 74:
                        Console.WriteLine("\tCode {0:000}: {1} - Программирование коммутации в блоке матрицы М201", (index), _codes[index]);
                        index++;
                        while (!((_codes[index].Substring(2,1) == "2") || (_codes[index].Substring(2,1) == "6")))
                        {
                            Console.WriteLine("\t\tMatrix {0:000}: {1} - OK", (index), _codes[index]);
                            _io.Matrix(_codes[index]);
                            index++;
                        }

                        var code = new System.Text.StringBuilder(_codes[index]);
                        switch (_codes[index][2])
                        {
                            case '2':
                                code[2] = '0';
                                break;
                            case '6':
                                code[2] = '4';
                                break;
                        }
                        Console.WriteLine("\t\tMatrix {0:000}: {1} => {2} - OK", (index), _codes[index], code);
                       _io.Matrix(code.ToString());

                        // Задержка на коммутацию реле 3 мс.
                        Thread.Sleep(_io.matrixDelay);
                        index++;
                        break;

                    // "4000" - Включение вентилей источников в блоках И203 и В202.
                    case 40:
                        Console.WriteLine("\tCommand {0:000}: {1} - Включение вентилей источников в блоках И203 и В202", (index), _codes[index]);
                        _io.Command(_codes[index]);
                        index++;
                        Console.WriteLine("\t\tValue {0:000}: {1} - OK", (index), _codes[index]);
                        _io.Value(_codes[index]);
                        index++;
                        break;

                    // "41XX", "42XX" - Программирование цифрового измерительного блока В202.
                    case 41:
                    case 42:
                        Console.WriteLine("\tCommand {0:000}: {1} - Программирование цифрового измерительного блока В202", (index), _codes[index]);
                        _io.Command(_codes[index]);
                        index++;
                        Console.WriteLine("\t\tValue {0:000}: {1} - OK", (index), _codes[index]);
                        _io.Value(_codes[index]);
                        index++;                        
                        break;

                    // "4[3-7]XX", "51XX" - Программирование блоков источников И203.
                    case 43:
                    case 44:
                    case 45:
                    case 46:
                    case 47:
                    case 51:
                        Console.WriteLine("\tCommand {0:000}: {1} - Программирование блоков источников И203", (index), _codes[index]);
                        _io.Command(_codes[index]);
                        index++;
                        Console.WriteLine("\t\tValue {0:000}: {1} - OK", (index), _codes[index]);
                        _io.Value(_codes[index]);
                        index++;                        
                        break;

                    // "75XX" - Временная пауза.
                    case 75:
                        Console.WriteLine("\tCode {0:000}: {1} - Временная пауза", (index), _codes[index]);
                        Thread.Sleep(loWord == 0 ? 4096 : loWord);
                        index++;                        
                        break;

                    // "76XX" - Конец теста.
                    case 76:
                        Console.WriteLine("\tCode {0:000}: {1} - Код критерия годности", (index), _codes[index]);
                        index++;
                        Console.WriteLine("\t\tCode {0:000}: {1} - OK", (index), _codes[index]);
                        Thread.Sleep(_codes[index] == "0000" ? 4096 : int.Parse(_codes[index]));
                        index++;
                        break;

                    default:
                        Console.WriteLine("\tCode {0:000}: {1} - не распознан", (index), _codes[index]);
                        //index++;
                        return false;
                }
            }
            return true;
        }
    }

    public class DifferentialTest
    {

    }

    public class ClassificationTest
    {

    }

    public class CharacteristicTest
    {

    }

    /// <summary>
    /// Класс для работы с планами тестера.
    /// План представляет собой набор тестов.
    /// </summary>
    public class Plan
    {
        private IO.IntegralIO _io;
        private bool _loaded;
        private string _caption;
        private string _path;
        private List<Test> _tests;

        public string Caption
        {
            get { return _caption; }
            set { _caption = value; }
        }
        public string Path => _path;
        public bool Loaded => _loaded;
        public List<Test> Tests => _tests;

        public Plan(IO.IntegralIO testerIO)
        {
            _io      = testerIO;
            _loaded  = false;
            _caption = "";
            _path    = "";
            _tests   = new List<Test>();
        }

        public void Clear()
        {
            _loaded  = false;
            _caption = "";
            _path    = "";
            _tests.Clear();
        }

        /// <summary>
        /// Загрузка плана из файла
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns></returns>
        public bool LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return false;
            else
            {
                _path = filePath;
                var reader = File.OpenText(_path);
                _caption = reader.ReadLine()?.Substring(1).Trim();
                string line;
                while ((line = reader.ReadLine()?.Trim()) != null)
                {
                    var newtest = new Test(_io);
                    if (newtest.Parse(line) > 0)
                        _tests.Add(newtest);
                    else
                    {
                        Clear();
                        return false;
                    }
                }

                return true;
            }
        }

        public bool Execute()
        {
            foreach (var test in Tests)
            {
                Console.WriteLine("T" + test.Number);
                if (!test.Execute())
                    return false;
            }

            return true;
        }
    }

    public class Tester
    {
        private IO.IntegralIO _io;
        private System.Timers.Timer _timer;
        private Plan[] _plans;

        public Tester()
        {
            _io = new IO.IntegralIO();
            ResetTester();
            Command("0100");

            // Выделение памяти под 16 планов (0-ой план - пустой)
            _plans = new Plan[16];
            for (var i = 0; i < _plans.Length; i++)
                _plans[i] = new Plan(_io);

            _timer = new System.Timers.Timer(100);
            _timer.Elapsed += _timer_Elapsed;
            //_timer.Start();
        }

        /// <summary>
        /// Сброс тестера
        /// </summary>
        public void ResetTester()
        {
            Console.WriteLine("=> Сброс тестера");
            Command("0217");
        }

        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timer.Stop();

            byte planNumber = (byte)(_io.Data() & 0xF);
            if (planNumber == 0)
                Console.WriteLine("Выберите на пульте номер плана отличный от 0");
            else if (planNumber >= Plans.Length)
                Console.WriteLine("Недопустимый номер плана - {0}", planNumber);
            else
            {
                if (string.IsNullOrWhiteSpace(Plans[planNumber].Path))
                    Console.WriteLine("План {0} не загружен!", planNumber);
                else
                {
                    Console.WriteLine("Выполняется ПЛАН №{0}", planNumber);
                    if (!Plans[planNumber].Execute())
                        Console.WriteLine("Ошибка выполнения плана!");
                }
                Command("0100");
            }

            _timer.Start();
        }

        public Plan[] Plans => _plans;
        public void Command(string oct) { _io.Command(oct); }
        public void Value(string oct) { _io.Value(oct); }
        public void Matrix(string oct) { _io.Matrix(oct); }
        public ushort K100() { return _io.K100(); }
        public ushort K101() { return _io.K101(); }
    }
}

namespace Integral_Console
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Управляющая программа для тестера \"\"");
            Console.WriteLine("Параметры командной строки: Integral.exe [Файл для плана 1] ... [Файл для плана 15]\n");

            var tester = new Integral.Tester();
            for (var index = 0; index < (args.Length > tester.Plans.Length ? tester.Plans.Length : args.Length); index++)
            {
                Console.Write("ПЛАН №{0}: \"{1}\" - ", index + 1, Path.GetFullPath(args[index]));
                Console.WriteLine(tester.Plans[index + 1].LoadFromFile(args[index]) ? "Успешно" : "Ошибка");
            }

            tester.Plans[1].Execute();
            //Console.WriteLine("\nВыберите номер плана на пульте и нажмите ПУСК или ESC для выхода...");
            //do
            //{
            //    while (!Console.KeyAvailable)
            //    {
            //        // Do something
            //    }
            //} while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }
    }
}