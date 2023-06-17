using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;
using ConsoleTables;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Linq;

namespace WpfHalsted
{
    public partial class MainWindow : System.Windows.Window
    {
        // Инициализация окна
        public MainWindow()
        {
            InitializeComponent();
        }

        // Подсчет метрик для Python
        static int[] CountPython(string fileName)
        {
            int[] result = new int[4] { 0, 0, 0, 0}; // Массив для 4 метрик
            int uniqueOperators = 0;
            int allOperators = 0;
            int allOperands = 0;
            string commentPattern = @"((#.*)|('[^']*'|""[^""]*""))";

            string code = File.ReadAllText(fileName); // Cчитываем код из файла в строку
            code = Regex.Replace(code, commentPattern, String.Empty); 

            var operators = new HashSet<string> { "<=", ">=", "==", "!=", "+=", "-=", "/=", "*=", "%=", "**=", "//=",
                                                  "and ", "or ", "not in ","not ", "in ", "is ", "&", "|", "^", "~", "<<", ">>", 
                                                  "=", "+", "-", "//", "/", "**", "*", "%", "<", ">", "while", 
                                                  "for", "try", "with ", "[", "def ", "lambda ", "return ", "if"};

            if (code == "") return result; // Проверка на пустоту

            foreach (var elem in operators)
            {
                int i = 0;
                int count = 0; // Вспомогательная переменная для того, чтобы уникальный оператор подсчитывался единажды

                do
                {
                    i = code.IndexOf(elem, i + 1);

                    if (i != -1)
                    {
                        if (count == 0)
                        {
                            count++;
                            uniqueOperators++;
                        }

                        allOperators++;
                    }
                } while (i != -1);

                code = code.Replace(elem, " "); // Заменяем продсчитанный элемент на пробел
            }

            var extraOperators = new HashSet<string> { "while", "for", "try", "finally", "except", "with", "[", "def", "lambda", "return", "if", "else", "elif"};

            string[] splitCode = code.Split(new char[] { ';', ' ', '.', ':', '\n', '\r', '(', ')', '[', ']', '{', '}',
                                                         ',' , '\"', '\'', '\\', '@', '$'}, StringSplitOptions.RemoveEmptyEntries);

            splitCode = splitCode.Where(x => !extraOperators.Contains(x)).ToArray();//Удаление вторых частей из массива                  

            HashSet<string> UniqOperands = new HashSet<string>();
            
            for (int i = 0; i < splitCode.Length; i++)
            {
                string element = splitCode[i];
                allOperands++;

                if (!UniqOperands.Contains(element))
                {
                    UniqOperands.Add(element);
                }
            }

            result[0] = uniqueOperators;
            result[1] = UniqOperands.Count;
            result[2] = allOperators;
            result[3] = allOperands;

            return result;
        }

        // Работа с папками
        static void ReadFiles(string[] arrFiles)
        {
            var table = new ConsoleTable("Имя файла", "Кол-во строк", "Язык", "Число уникальных операторов (n1)", 
                                         "Число уникальных операндов (n2)", "Общее число операторов (N1)", 
                                         "Общее число операндов (N2)", "Словарь (n = n1 + n2)", 
                                         "Длина программы (N = N1 + N2)", "Объем программы (N * log2(n))", 
                                         "Сложность реализации ((n1*N2)/(2*n2))", "Трудность (n1/2 + N2/n2)"); // Создаём таблицу

            string path = "";

            foreach (string fileNames in arrFiles)
            {
                if (path == "") path = fileNames.Remove(fileNames.LastIndexOf(@"\"));
                int indexNameFile = fileNames.LastIndexOf(@"\"); // Находим индекс последнего \, для последующего выделения имени
                string fileName = fileNames.Remove(0, indexNameFile + 1); // Выделяем имя
                int lines = File.ReadAllLines(fileNames).Length; // Количество строк в файле

                switch (LanguageMetr(fileName))
                {
                    case "C#":
                        // Вызываем метод для подсчета метрик для языка C# и заполняем таблицу
                        FillTable(CountCS(fileNames), fileName, table, lines); 
                        break;

                    case "Python":
                        // Вызываем метод для подсчета метрик для языка Python и заполняем таблицу
                        FillTable(CountPython(fileNames), fileName, table, lines);
                        break;

                    case "Pascal":
                        // Вызываем метод для подсчета метрик для языка Pascal и заполняем таблицу
                        FillTable(CountPascal(fileNames), fileName, table, lines);
                        break;
                }
            }
            string tableStr = table.ToString();

            FillFile($"{tableStr}", path); // Вызываем метод для заполнения файла
        }

        // Проверка языка программирования
        static string LanguageMetr(string nameFile)
        {
            int indexNameFile = nameFile.LastIndexOf(@"."); // Находим индекс последнего . для последующего выделения расширения
            string fileName = nameFile.Remove(0, indexNameFile + 1); // Выделяем расширение

            Dictionary<string, string> pickFileName = new Dictionary<string, string>() // Словарь с расширениями файла
            {
                { "cs", "C#"},
                { "py", "Python"},
                { "pas", "Pascal"}
            };

            if (pickFileName.ContainsKey(fileName)) // Проверяем подходит ли нам файл
            {
                return pickFileName[fileName];
            }
            else
            {
                return "NONE";
            }
        }

        // Запись файла
        static void FillFile(string info, string path)
        {
            path += $"\\The Result Of The Calculation Of Halsted Metrics.txt";

            using (FileStream fstream = new FileStream(path, FileMode.Create))
            {
                byte[] bytes = Encoding.Default.GetBytes(info);

                fstream.Write(bytes, 0, bytes.Length);
            }

            System.Windows.MessageBox.Show($"Итоги записаны в файл путь до которого: {path}");
        }

        // Подсчет метрик для C#
        static int[] CountCS(string fileName)
        {
            int[] result = new int[4] { 0, 0, 0, 0}; // Массив для 4 метрик
            int allOperators = 0;
            int uniqueOperators = 0;
            int allOperands = 0;

            string code = File.ReadAllText(fileName); // Cчитываем код из файла в строку
            code = Regex.Replace(code, @"(?s)\s*\/\/.+?\n|\/\*.*?\*\/\s*", String.Empty);

            var operators = new HashSet<string> { "??=", "??", "?.", "?[", "++", "--", "==", "!=", "&&", "||", "+=", "-=", 
                                                  "*=", "->", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", "%", "+", "-", 
                                                  "*", "/", "&", "|", "^", "!", "~", "<<<", ">>>", "<<", "=>", ">>", "<", 
                                                  ">", "?", "::", ":", "=", "..", ".", "[", "is ", "as ", "typeof ", 
                                                  "op ", "switch ", "case ", "try", "while", "await ", "default ", 
                                                  "delegate ", "new ", "sizeof", "with ", "nameof", "for", 
                                                  "foreach", "throw ", "break", "continue", "yield ", 
                                                  "return", "if"};

            if (code == "") return result; // Проверка на пустоту

            foreach (var elem in operators)
            {
                int i = -1;
                int count = 0; // Вспомогательная переменная для того, чтобы уникальный оператор подсчитывался единажды

                do
                {
                    i = code.IndexOf(elem, i + 1);

                    if (i != -1)
                    {
                        if (count == 0)
                        {
                            count++;
                            uniqueOperators++;
                        }

                        allOperators++;
                    }
                } while (i != -1);

                code = code.Replace(elem, " "); // Заменяем продсчитанный элемент на пробел
            }

            var extraOperators = new HashSet<string> { "do", "catch", "finally", "is ", "as ", "typeof ", "else",
                                                       "op", "switch", "case", "try", "while", "await", "default",
                                                       "delegate", "new", "sizeof", "with", "nameof", "for", "if",
                                                       "foreach", "throw", "break", "continue", "yield", "return"};


            var UniqOperands = new HashSet<string>();

            string[] splitCode = code.Split(new char[] { ';', ' ', '.', ':', '\n', '\r', '(', ')', '[', ']', '{', '}',
                                                         ',' , '\"', '\'', '\\', '@', '$'}, StringSplitOptions.RemoveEmptyEntries);

            splitCode = splitCode.Where(x => !extraOperators.Contains(x)).ToArray();//Удаление вторых частей из массива

            for (int i = 0; i < splitCode.Length; i++)
            {
                string element = splitCode[i];
                allOperands++;

                if (!UniqOperands.Contains(element))
                {
                    UniqOperands.Add(element);
                }
            }

            result[0] = uniqueOperators;
            result[1] = UniqOperands.Count;
            result[2] = allOperators;
            result[3] = allOperands;

            return result;
        }

        // Подсчет метрик для Pascal
        static int[] CountPascal(string fileName)
        {
            int[] result = new int[4] { 0, 0, 0, 0 }; // Массив для 4 метрик
            int allOperators = 0;
            int uniqueOperators = 0;
            int allOperands = 0;
            string commentPattern1 = @"\{.*?\}";
            string commentPattern2 = "@\"//.*\"";
            string commentPattern3 = @"(\*.*?\*)";


            string code = File.ReadAllText(fileName); // Cчитываем код в строку с файла
            code = Regex.Replace(code, commentPattern1, String.Empty, RegexOptions.Singleline);
            code = Regex.Replace(code, commentPattern2, String.Empty);
            code = Regex.Replace(code, commentPattern3, String.Empty, RegexOptions.Singleline);

            var operators = new HashSet<string> { "<=", ">=", "=", "<>", "and ", "or ", "not ", "case ", "with ", "if", "repeat",
                                                  "for", "while", ":=", "+", "-", "div ", "/", "*", "mod ", "<", ">",};

            if (code == "") return result; // Проверка на пустоту

            foreach (var elem in operators)
            {
                int i = -1;
                int count = 0; // Вспомогательная переменная для того, чтобы уникальный оператор подсчитывался единажд

                do
                {
                    i = code.IndexOf(elem, i + 1);

                    if (i != -1)
                    {
                        if (count == 0)
                        {
                            count++;
                            uniqueOperators++;
                        }

                        allOperators++;
                    }
                } while (i != -1);

                code = code.Replace(elem, " "); // Заменяем продсчитанный элемент на пробел
            }

            string[] splitCode = code.Split(new char[] { ';', ' ', '.', ':', '(', ')', '[', ']', '{', '}', 
                                                         ',' }, StringSplitOptions.RemoveEmptyEntries);

            HashSet<string> UniqOperands = new HashSet<string>();

            for (int i = 0; i < splitCode.Length; i++)
            {
                string element = splitCode[i];
                allOperands++;

                if (!UniqOperands.Contains(element))
                {
                    UniqOperands.Add(element);
                }
            }

            result[0] = uniqueOperators;
            result[1] = UniqOperands.Count;
            result[2] = allOperators;
            result[3] = allOperands;

            return result;
        }

        // Заполнение таблицы готовыми результатами
        static void FillTable(int[] metrics, string fileName, ConsoleTable table, int lines)
        {
            int[] OperstorsOperands = metrics;
            int uniqueOperators = OperstorsOperands[0];
            int uniqueOperands = OperstorsOperands[1];
            int allOperators = OperstorsOperands[2];
            int allOperands = OperstorsOperands[3];

            int dictionary = uniqueOperators + uniqueOperands;
            int duration = allOperators + allOperands;
            double volume = 0;

            if (dictionary != 0) volume = duration * Math.Log(dictionary, 2); ;

            double complexity = 0;
            double difficulty = 0;

            if (uniqueOperands != 0)
            {
                complexity = (uniqueOperators * (double)allOperands) / (2.0 * uniqueOperands);
                difficulty = (uniqueOperators / 2.0) + ((double)allOperands / uniqueOperands);
            }

            table.AddRow(fileName, lines, LanguageMetr(fileName), uniqueOperators, uniqueOperands, allOperators,
                         allOperands, dictionary, duration, Math.Round(volume, 3), Math.Round(complexity, 3),
                         Math.Round(difficulty, 3)); // Добавляем ряд в таблицу
        }

        private void Button_Count_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

                dlg.Filter = "Py, Cs and Pas Files (*.py;*.cs;*.pas)|*.py;*.cs;*.pas|" +
                             "Cs Files (*.cs)|*.cs|" +
                             "Py Files (*.py)|*.py|" +
                             "Pascal Files (*.pas)|*.pas";

                dlg.Multiselect = true;

                if (dlg.ShowDialog() == true)
                {
                    ReadFiles(dlg.FileNames);
                }
            }
            catch
            {
                System.Windows.MessageBox.Show("Что-то пошло не так! Свяжитесь с службой поддержки, данные которой есть в справке!");
            }
        }

        private void Button_Help_Click(object sender, RoutedEventArgs e)
        {
            string commandText = @"Help.chm";

            System.Windows.Forms.Help.ShowHelp(null, commandText);
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            string commandText = @"Help.chm";

            if (e.Key == Key.F1)
            {
                System.Windows.Forms.Help.ShowHelp(null, commandText, HelpNavigator.TopicId, "12");
            }
        }
    }
}
