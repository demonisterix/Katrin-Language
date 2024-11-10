using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace KatrinEngine.CVNAF
{
    public class CVNAFile
    {
        private string _filePath;
        private Dictionary<string, object> _files;

        public CVNAFile(string filePath)
        {
            _filePath = filePath;
            _files = new Dictionary<string, object>();
        }

        private struct CVNAFHeader
        {
            public byte[] Signature;
            public int Version;
            public int FileCount;
        }

        // Свойство FilePath
        public string FilePath
        {
            get { return _filePath; }
        }

        private int GetFileCount(Dictionary<string, object> files)
        {
            int count = 0;
            foreach (var file in files)
            {
                if (file.Value is byte[])
                {
                    count++;
                }
                else if (file.Value is Dictionary<string, object> subFiles)
                {
                    count += GetFileCount(subFiles);
                }
            }
            return count;
        }

        public void AddFile(string fileName, byte[] fileData)
        {
            // Разделение пути на части, чтобы получить имя папки
            string[] parts = fileName.Split('/');
            string folderName = string.Join("/", parts.Take(parts.Length - 1));
            string file = parts.Last();

            // Получение словаря для данной папки
            var folderDict = GetFolderDictionary(folderName);

            // Добавление файла в словарь папки
            folderDict[file] = fileData;
        }

        // Метод для создания архива
        public void CreateArchive(string archiveFilePath, string directoryPath)
        {
            // Проверка, существует ли директория
            if (!Directory.Exists(directoryPath))
            {
                throw new ArgumentException("Директория не найдена: " + directoryPath);
            }

            // Создание объекта CVNAFile
            CVNAFile archive = new CVNAFile(archiveFilePath);

            // Архивирование директории
            archive.ArchiveSetDirectory(directoryPath);

            // Сохранение архива
            archive.Save();
        }

        public void RemoveFile(string fileName)
        {
            // Разделение пути на части, чтобы получить имя папки
            string[] parts = fileName.Split('/');
            string folderName = string.Join("/", parts.Take(parts.Length - 1));
            string file = parts.Last();

            // Получение словаря для данной папки
            var folderDict = GetFolderDictionary(folderName);

            // Удаление файла из словаря
            if (folderDict.ContainsKey(file))
            {
                folderDict.Remove(file);
            }
        }

        public byte[] GetFile(string fileName)
        {
            // Разделение пути на части, чтобы получить имя папки
            string[] parts = fileName.Split('/');
            string folderName = string.Join("/", parts.Take(parts.Length - 1));
            string file = parts.Last();

            // Получение словаря для данной папки
            var folderDict = GetFolderDictionary(folderName);

            // Проверка, существует ли файл в словаре
            if (folderDict.ContainsKey(file) && folderDict[file] is byte[] fileData)
            {
                return fileData;
            }

            return null;
        }

        // Метод GetFiles
        public List<string> GetFiles()
        {
            List<string> filePaths = new List<string>();

            foreach (var file in _files)
            {
                filePaths.Add(file.Key);
            }

            return filePaths;
        }

        public void ArchiveSetDirectory(string directoryPath)
        {
            // Получение списка файлов и поддиректорий в директории
            var filesAndDirectories = Directory.EnumerateFileSystemEntries(directoryPath);

            // Проход по каждому файлу и поддиректории
            foreach (var item in filesAndDirectories)
            {
                // Проверка, является ли элемент файлом
                if (File.Exists(item))
                {
                    // Чтение файла в байтовый массив
                    byte[] fileData = File.ReadAllBytes(item);

                    // Получение относительного пути файла
                    string relativePath = item.Substring(directoryPath.Length + 1);

                    // Добавление файла в архив
                    AddFile(relativePath, fileData);
                }
                else if (Directory.Exists(item))
                {
                    // Рекурсивный вызов метода ArchiveSetDirectory для поддиректории
                    ArchiveSetDirectory(item);
                }
            }
        }

        // Метод для сохранения архива в файл
        public void Save()
        {
            // Создание временного файла
            string tempFilePath = Path.GetTempFileName();

            // Создание потока для записи в файл
            using (FileStream outputStream = new FileStream(tempFilePath, FileMode.Create))
            {
                // Создание заголовка
                CVNAFHeader header = new CVNAFHeader();
                header.Signature = Encoding.ASCII.GetBytes("CVNAF");
                header.Version = 1;
                header.FileCount = GetFileCount(_files);

                // Запись заголовка в файл
                outputStream.Write(header.Signature, 0, header.Signature.Length);
                outputStream.Write(BitConverter.GetBytes(header.Version), 0, sizeof(int));
                outputStream.Write(BitConverter.GetBytes(header.FileCount), 0, sizeof(int));

                // Запись данных файлов в файл
                WriteFiles(outputStream, _files);
            }

            // Перемещение временного файла в конечный файл
            File.Move(tempFilePath, _filePath);
        }

        // Метод для загрузки архива из файла
        public void Load()
        {
            // Чтение заголовка
            using (FileStream inputStream = new FileStream(_filePath, FileMode.Open))
            {
                // Чтение сигнатуры
                byte[] signature = new byte[5];
                inputStream.Read(signature, 0, signature.Length);

                // Проверка сигнатуры
                if (!Encoding.ASCII.GetString(signature).Equals("CVNAF"))
                {
                    throw new Exception("Некорректный файл CVNAF.");
                }

                // Чтение версии
                int version = BitConverter.ToInt32(ReadBytes(inputStream, 4));

                // Чтение количества файлов
                int fileCount = BitConverter.ToInt32(ReadBytes(inputStream, 4));

                // Чтение данных файлов
                _files = ReadFiles(inputStream);
            }
        }

        // Метод для записи файлов в поток
        private void WriteFiles(Stream outputStream, Dictionary<string, object> files)
        {
            foreach (var file in files)
            {
                if (file.Value is byte[] fileData)
                {
                    // Запись имени файла
                    byte[] fileNameBytes = Encoding.UTF8.GetBytes(file.Key);
                    outputStream.Write(BitConverter.GetBytes(fileNameBytes.Length), 0, sizeof(int));
                    outputStream.Write(fileNameBytes, 0, fileNameBytes.Length);

                    // Запись данных файла
                    outputStream.Write(BitConverter.GetBytes(fileData.Length), 0, sizeof(int));
                    outputStream.Write(fileData, 0, fileData.Length);
                }
                else if (file.Value is Dictionary<string, object> subFiles)
                {
                    // Запись имени папки
                    byte[] folderNameBytes = Encoding.UTF8.GetBytes(file.Key);
                    outputStream.Write(BitConverter.GetBytes(folderNameBytes.Length), 0, sizeof(int));
                    outputStream.Write(folderNameBytes, 0, folderNameBytes.Length);

                    // Рекурсивный вызов метода WriteFiles для поддиректории
                    WriteFiles(outputStream, subFiles);
                }
            }
        }

        // Метод для чтения файлов из потока
        private Dictionary<string, object> ReadFiles(Stream inputStream)
        {
            Dictionary<string, object> files = new Dictionary<string, object>();

            // Чтение всех файлов
            while (inputStream.Position < inputStream.Length)
            {
                // Чтение длины имени файла или папки
                int nameLength = BitConverter.ToInt32(ReadBytes(inputStream, 4));

                // Чтение имени файла или папки
                byte[] nameBytes = ReadBytes(inputStream, nameLength);
                string name = Encoding.UTF8.GetString(nameBytes);

                // Чтение длины данных файла или папки
                int dataLength = BitConverter.ToInt32(ReadBytes(inputStream, 4));

                // Чтение данных файла или папки
                byte[] data = ReadBytes(inputStream, dataLength);

                if (dataLength > 0)
                {
                    // Добавление файла в словарь
                    files[name] = data;
                }
                else
                {
                    // Создание словаря для поддиректории
                    Dictionary<string, object> subFiles = ReadFiles(inputStream);
                    files[name] = subFiles;
                }
            }

            return files;
        }

        // Метод для чтения байтов из потока
        private byte[] ReadBytes(Stream inputStream, int length)
        {
            byte[] buffer = new byte[length];
            inputStream.Read(buffer, 0, length);
            return buffer;
        }

        // Метод для получения словаря для данной папки
        private Dictionary<string, object> GetFolderDictionary(string folderName)
        {
            Dictionary<string, object> currentDict = _files;
            string[] folderParts = folderName.Split('/');

            foreach (string folderPart in folderParts)
            {
                if (!currentDict.ContainsKey(folderPart))
                {
                    currentDict[folderPart] = new Dictionary<string, object>();
                }

                currentDict = currentDict[folderPart] as Dictionary<string, object>;
            }

            return currentDict;
        }

        // Метод для открытия архива
        public void OpenArchive(string archiveFilePath)
        {
            // Проверка, существует ли файл архива
            if (!File.Exists(archiveFilePath))
            {
                throw new ArgumentException("Файл архива не найден: " + archiveFilePath);
            }

            // Загрузка архива
            Load();
        }

        // Метод для сохранения архива
        public void SaveArchive(string archiveFilePath)
        {
            // Проверка, существует ли файл архива
            if (!File.Exists(archiveFilePath))
            {
                throw new ArgumentException("Файл архива не найден: " + archiveFilePath);
            }

            // Сохранение архива
            Save();
        }

        // Метод для добавления файла в архив
        public void AddFileToArchive(string fileName, byte[] fileData)
        {
            // Добавление файла в архив
            AddFile(fileName, fileData);
        }

        // Метод для создания папки в архиве
        public void CreateFolderInArchive(string folderName)
        {
            // Создание папки в архиве
            GetFolderDictionary(folderName); // Это создаст папку, если она не существует
        }

        // Метод для получения списка файлов и папок в архиве
        public List<string> GetArchiveFiles()
        {
            List<string> files = new List<string>();
            GetFilesRecursive(_files, "", files);
            return files;
        }

        // Вспомогательный метод для рекурсивного получения списка файлов
        private void GetFilesRecursive(Dictionary<string, object> files, string path, List<string> result)
        {
            foreach (var file in files)
            {
                string fullPath = Path.Combine(path, file.Key);

                if (file.Value is byte[])
                {
                    result.Add(fullPath);
                }
                else if (file.Value is Dictionary<string, object> subFiles)
                {
                    GetFilesRecursive(subFiles, fullPath, result);
                }
            }
        }
    }
}