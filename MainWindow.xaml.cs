using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ConsoleTables;
using System.IO;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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
            int[] result = new int[4] { 0, 0, 0, 0}; // массив для 4 метрик
            int uniqueOperators = 0;
            int uniqueOperands = 0;
            int allOperators = 0;
            int allOperands = 0;

            string code = File.ReadAllText(fileName); // считываем код в строку с файла
            code = Regex.Replace(code, @"(?s)\s*\/\/.+?\n|\/\*.*?\*\/\s*", String.Empty); // проверить как пишутся коменты в питоне

            var operators = new HashSet<string> { "<=", ">=", "==", "!=", "+=", "-=", "/=", "*=", "%=", "**=", "//=",
                                                  "and ", "or ", "not ", "in ", "is ", "&", "|", "^", "~", "<<", ">>", 
                                                  "=", "+", "-", "//", "/", "**", "*", "%", "<", ">",};

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

            string[] splitCode = code.Split(new char[] { ';', ' ', '.', ':', '\n', '\r', '(', ')', 
                                                         '[', ']', '{', '}', ',' }, StringSplitOptions.RemoveEmptyEntries);                      

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

            uniqueOperands = UniqOperands.Count;

            result[0] = uniqueOperators;
            result[1] = uniqueOperands;
            result[2] = allOperators;
            result[3] = allOperands;

            return result;
        }

        /// <summary>
        ///Работа с файлом
        /// </summary>
        /// <param name="path"></param>
        static void ReadFile(string path)
        {
            int lines = File.ReadAllLines(path).Length; // Количество строк в файле
            int indexNameFile = path.LastIndexOf(@"\"); // Находим индекс последнего \, для последующего выделения имени
            string fileName = path.Remove(0, indexNameFile + 1); // Выделяем имя

            var table = new ConsoleTable("Имя файла", "Кол-во строк", "Язык", "Число уникальных операторов (n1)", 
                                         "Число уникальных операндов (n2)", "Общее число операторов (N1)", 
                                         "Общее число операндов (N2)", "Словарь (n = n1 + n2)", "Длина программы (N = N1 + N2)", 
                                         "Объем программы (N * log2(n))", "Сложность реализации ((n1*N2)/(2*n2))", 
                                         "Трудность (n1/2 + N2/n2)"); // Создаём таблицу

            Console.WriteLine();

            if (LanguageMetr(fileName) == "C#")
            {
                int[] OperstorsOperands = CountCS(path);
                int uniqueOperators = OperstorsOperands[0];
                int uniqueOperands = OperstorsOperands[1];
                int allOperators = OperstorsOperands[2];
                int allOperands = OperstorsOperands[3];

                int dictionary = uniqueOperators + uniqueOperands;
                int duration = allOperators + allOperands;
                double volume = 0;

                if (dictionary != 0) volume = duration * Math.Log(dictionary, 2);

                double complexity = 0;
                double difficulty = 0;

                if (uniqueOperands != 0)
                {
                    complexity = (uniqueOperators * (float)allOperands) / (2.0 * uniqueOperands);
                    difficulty = (uniqueOperators / 2) + (allOperands / uniqueOperands);
                }

                table.AddRow(fileName, lines, LanguageMetr(fileName), uniqueOperators, uniqueOperands, allOperators, 
                             allOperands, dictionary, duration, Math.Round(volume, 3), Math.Round(complexity, 3), 
                             Math.Round(difficulty, 3)); // Добавляем ряд в таблицу
            }
            else if (LanguageMetr(fileName) == "Python")
            {
                int[] OperstorsOperands = CountPython(path);
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
                    difficulty = (uniqueOperators / 2.0) + (allOperands / uniqueOperands);
                }

                table.AddRow(fileName, lines, LanguageMetr(fileName), uniqueOperators, uniqueOperands, allOperators, allOperands, 
                             dictionary, duration, Math.Round(volume, 3), Math.Round(complexity,3), 
                             Math.Round(difficulty,3)); // Добавляем ряд в таблицу
            }

            FillFile(table.ToString(), System.IO.Path.GetDirectoryName(path));
        }

        /// <summary>
        /// Работа с папками
        /// </summary>
        /// <param name="path"></param>
        static void ReadDirectory(string[] arrFiles)
        {
            var table = new ConsoleTable("Имя файла", "Кол-во строк", "Язык", "Число уникальных операторов (n1)", 
                                         "Число уникальных операндов (n2)", "Общее число операторов (N1)", "Общее число операндов (N2)",
                                         "Словарь (n = n1 + n2)", "Длина программы (N = N1 + N2)", "Объем программы (N * log2(n))", 
                                         "Сложность реализации ((n1*N2)/(2*n2))", "Трудность (n1/2 + N2/n2)"); // Создаём таблицу

            string path = "";

            foreach (string fileNames in arrFiles)
            {
                if (path == "") path = fileNames.Remove(fileNames.LastIndexOf(@"\")); ;
                int indexNameFile = fileNames.LastIndexOf(@"\"); // Находим индекс последнего \, для последующего выделения имени
                string fileName = fileNames.Remove(0, indexNameFile + 1); // Выделяем имя
                int lines = File.ReadAllLines(fileNames).Length; // Количество строк в файле

                if (LanguageMetr(fileNames) == "C#")
                {
                    int[] OperstorsOperands = CountCS(fileNames);
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

                    table.AddRow(fileName, lines, LanguageMetr(fileName), uniqueOperators, uniqueOperands, allOperators, allOperands, dictionary, duration, Math.Round(volume, 3), Math.Round(complexity, 3), Math.Round(difficulty, 3)); // Добавляем ряд в таблицу
                }
                else if (LanguageMetr(fileNames) == "Python")
                {
                    int[] OperstorsOperands = CountPython(fileNames);
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
                        difficulty = (uniqueOperators / 2.0) + ((double)allOperands / uniqueOperands);;
                    }

                    table.AddRow(fileName, lines, LanguageMetr(fileName), uniqueOperators, uniqueOperands, allOperators, allOperands, dictionary, duration, Math.Round(volume, 3), Math.Round(complexity, 3), Math.Round(difficulty, 3)); // Добавляем ряд в таблицу
                }
            }
            string tableStr = table.ToString();

            FillFile($"{tableStr}", path);
        }

        /// <summary>
        /// Проверка языка программирования
        /// </summary>
        /// <param name="nameFile"></param>
        /// <returns></returns>
        static string LanguageMetr(string nameFile)
        {
            int indexNameFile = nameFile.LastIndexOf(@"."); // Находим индекс последнего ., для последующего выделения расширения
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

        /// <summary>
        /// Запись файла
        /// </summary>
        /// <param name="info"></param>
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

        static int[] CountCS(string fileName)
        {
            int[] result = new int[4] { 0, 0, 0, 0};
            int allOperators = 0;
            int uniqueOperators = 0;
            int allOperands = 0;

            string code = File.ReadAllText(fileName);
            code = Regex.Replace(code, @"(?s)\s*\/\/.+?\n|\/\*.*?\*\/\s*", String.Empty);          

            var operators = new HashSet<string> { "++", "--", "==", "!=", "&&", "||", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", 
                                                  "<<=", ">>=", "%", "+", "-", "*", "/", "&", "|", "^", "!", "~", "<", ">", "?", 
                                                  ":", "="};

            if (code == "") return result;

            foreach (var elem in operators)
            {
                int i = -1;
                int c = 0;
                do
                {
                    i = code.IndexOf(elem, i + 1);
                    if (i != -1)
                    {
                        if (c == 0)
                        {
                            c++;
                            uniqueOperators++;
                        }

                        allOperators++;
                    }
                } while (i != -1);

                code = code.Replace(elem, " ");
            }

            string[] splitCode = code.Split(new char[] { ';', ' ', '.', ':', '\n', '\r', '(', ')', '[', ']', '{', '}', 
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

        private void Button_Count_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

                dlg.Filter = "Py, Cs and Pas Files (*.py;*.cs;*.pas)|*.py;*.cs;*.pas|Cs Files (*.cs)|*.cs|Py Files (*.py)|*.py|Pascal Files (*.pas)|*.pas";

                dlg.Multiselect = true;

                if (dlg.ShowDialog() == true)
                {
                    if(dlg.FileNames.Length > 1)
                    {
                        ReadDirectory(dlg.FileNames);
                    }
                    else
                    {
                        string filename = dlg.FileName;
                        ReadFile(filename);
                    }
                }
            }
            catch
            {
                System.Windows.MessageBox.Show("Что-то пошло не так! Свяжитесь с службой поддержки!");
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
