﻿using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace Changes_in_the_user
{
    internal class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.SetWindowSize(90, 30);
                Console.SetBufferSize(90, 30);
                Console.Clear(); // Очищаем экран перед каждой итерацией

                // Проверяем, запущена ли программа от имени администратора
                if (!IsAdministrator())
                {
                    RestartAsAdmin();
                    return;
                }

                // Получаем список пользователей из реестра
                string[] users = GetUserNames();

                if (users.Length == 0)
                {
                    Console.WriteLine("Не удалось найти пользователей.");
                    return;
                }

                // Выводим список пользователей
                Console.WriteLine("Список пользователей:");
                for (int i = 0; i < users.Length; i++)
                {
                    Console.WriteLine($"{i + 1}. {users[i]}");
                }

                // Предлагаем выбрать пользователя
                Console.WriteLine("\nВыберите номер пользователя: ");
                if (int.TryParse(Console.ReadLine(), out int userIndex) && userIndex > 0 && userIndex <= users.Length)
                {
                    //Console.Clear();
                    Console.WriteLine("");
                    string selectedUser = users[userIndex - 1];
                    Console.WriteLine($"Выбран пользователь: {selectedUser}");

                    // Получаем тип данных и ключ пользователя через экспорт ветки реестра
                    string userKeyLocation = GetUserKeyWithHex(selectedUser);
                    if (!string.IsNullOrEmpty(userKeyLocation))
                    {
                        Console.WriteLine($"Расположение ключа юзера: {userKeyLocation}");

                        // Выбор дальнейших действий
                        bool exitMenu = false;
                        while (!exitMenu)
                        {
                            
                            Console.WriteLine("\nВыберите действие:");
                            Console.WriteLine("1. Сбросить пароль");
                            Console.WriteLine("2. Узнать подсказки");
                            Console.WriteLine("3. Выбор пользователя");

                            string action = Console.ReadLine();

                            switch (action)
                            {
                                case "1":
                                    Console.WriteLine("ОК");
                                    Console.ReadLine(); // Ждем, чтобы пользователь увидел сообщение
                                    exitMenu = true;
                                    break;

                                case "2":
                                    Console.WriteLine("ОК");
                                    Console.ReadLine(); // Ждем, чтобы пользователь увидел сообщение
                                    exitMenu = true;
                                    break;

                                case "3":
                                    exitMenu = true; // Выход из текущего цикла меню, вернёмся к выбору пользователей
                                    break;

                                default:
                                    Console.WriteLine("Неверный выбор. Попробуйте снова.");
                                    Console.ReadLine(); // Ждем, чтобы пользователь увидел сообщение
                                    break;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Не удалось найти ключ пользователя.");
                    }
                }
                else
                {
                    Console.WriteLine("Неверный выбор.");
                }
            }
            }


            // Проверка, запущена ли программа с правами администратора
            static bool IsAdministrator()
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }

        // Перезапуск программы с правами администратора
        static void RestartAsAdmin()
        {
            var proc = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Process.GetCurrentProcess().MainModule.FileName,
                Verb = "runas"
            };
            try
            {
                Process.Start(proc);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при попытке запустить от администратора: {ex.Message}");
            }
        }

        // Получение имен пользователей из реестра
        static string[] GetUserNames()
        {
            string keyPath = @"SAM\SAM\Domains\Account\Users\Names";
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath))
                {
                    if (key != null)
                    {
                        return key.GetSubKeyNames();
                    }
                    else
                    {
                        Console.WriteLine("Не удалось открыть раздел реестра.");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Ошибка доступа. Для чтения этого раздела необходимы права администратора.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при чтении реестра: {ex.Message}");
            }
            return new string[0];
        }

        // Получение полного ключа через экспорт ветки реестра
        static string GetUserKeyWithHex(string userName)
        {
            string keyPath = $@"HKEY_LOCAL_MACHINE\SAM\SAM\Domains\Account\Users\Names\{userName}";
            string tempFile = Path.Combine(Path.GetTempPath(), $"{userName}_reg_export.reg");

            try
            {
                // Выполняем экспорт ветки реестра в файл
                var exportProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "reg.exe",
                        Arguments = $"export \"{keyPath}\" \"{tempFile}\" /y",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                exportProcess.Start();
                exportProcess.WaitForExit();

                if (!File.Exists(tempFile))
                {
                    Console.WriteLine("Ошибка экспорта ветки реестра.");
                    return null;
                }

                // Читаем файл и извлекаем hex значение
                string[] lines = File.ReadAllLines(tempFile);
                foreach (string line in lines)
                {
                    if (line.StartsWith("@=hex("))
                    {
                        // Извлекаем содержимое скобок
                        string hexValue = line.Split('(')[1].Split(')')[0].Trim();

                        // Преобразуем в формат 000003ED
                        string hexPadded = hexValue.PadLeft(8, '0').ToUpper();

                        // Собираем финальный путь
                        return $@"HKEY_LOCAL_MACHINE\SAM\SAM\Domains\Account\Users\{hexPadded}";
                    }
                }

                Console.WriteLine("Не удалось найти корректный тип данных в экспортированном файле.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при экспорте и чтении реестра: {ex.Message}");
            }
            finally
            {
                // Удаляем временный файл
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }

            return null;
        }
    }
}
