using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace UgadaikaClientWpf.Windows
{
    /// <summary>
    /// Окно для ввода хода игрока (буквы или слова)
    /// </summary>
    public partial class TurnWindow : Window
    {
        /// <summary>
        /// Регулярное выражение для проверки русских букв
        /// </summary>
        [GeneratedRegex("^[А-Яа-яЁё]+$")]
        private static partial Regex AllowedRegex();

        /// <summary>
        /// Объект для передачи ответа обратно
        /// </summary>
        private readonly AnswerString _answer;

        public TurnWindow(string description,
            string starredWord,
            string usedChars,
            AnswerString answer)
        {
            InitializeComponent();
            // Установка переданных данных
            Description.Text = description;      // Подсказка к слову
            StarredWord.Text = starredWord;      // Текущее состояние слова (например "к*т")
            UsedChars.Text = usedChars;          // Уже использованные буквы
            _answer = answer;                    // Объект для возврата ответа
        }

        /// <summary>
        /// Отправить символ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SendChar(object sender, RoutedEventArgs e)
        {
            if (PlayerAnswer.Text.Length == 1)
            {
                _answer.Answer = PlayerAnswer.Text; // Сохраняем букву
                Close();//закрыть окно
            }
            else
            {
                MessageBox.Show(Application.Current.MainWindow, "Для данного варианта необходимо указать 1 символ");
            }
        }

        /// <summary>
        /// Отправить слово целиком
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SendWord(object sender, RoutedEventArgs e)
        {
            if (PlayerAnswer.Text.Length == StarredWord.Text.Length)
            {
                _answer.Answer = PlayerAnswer.Text;// Сохраняем букву
                Close();//закрыть окно
            }
            else
            {
                MessageBox.Show(Application.Current.MainWindow, "Отправленное слово должно быть длиной идентично загаданному");
            }
        }

        /// <summary>
        /// Проверка введенного значения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckInputedValue(object sender, TextCompositionEventArgs e)
        {
            string input = e.Text;
            var notStarSymbolsResolved = StarredWord.Text.Where(c => c != '*').ToArray();// Получаем уже открытые буквы (не '*')
            /// Запрещаем:
            /// 1. Буквы, которые уже использовались
            /// 2. Буквы, которые уже открыты в слове
            e.Handled = !AllowedRegex().IsMatch(input) || input.Except(notStarSymbolsResolved).Any(UsedChars.Text.Contains);
        }
    }
}
