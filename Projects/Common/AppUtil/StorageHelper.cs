using System;
using System.Net;
using System.IO;
using System.IO.IsolatedStorage;
using System.Xml.Serialization;

namespace Naboo.AppUtil
{
    public class StorageHelper
    {
        public static bool SaveObject<T>(String filename, T content)
        {
            TextWriter writer = null;

            try
            {
                String oldFile = filename + ".old";
                String newFile = filename + ".new";

                IsolatedStorageFile isoStorage = IsolatedStorageFile.GetUserStoreForApplication();

                if (!isoStorage.DirectoryExists(Path.GetDirectoryName(newFile)))
                {
                    isoStorage.CreateDirectory(Path.GetDirectoryName(newFile));
                }

                IsolatedStorageFileStream file = isoStorage.OpenFile(newFile, FileMode.Create);
                writer = new StreamWriter(file);
                XmlSerializer xs = new XmlSerializer(typeof(T));
                xs.Serialize(writer, content);
                writer.Close();

                if (isoStorage.FileExists(filename))
                {
                    if (isoStorage.FileExists(oldFile))
                    {
                        isoStorage.DeleteFile(oldFile);
                    }

                    isoStorage.MoveFile(filename, oldFile);
                }

                isoStorage.MoveFile(newFile, filename);

                return true;
            }
            catch (Exception e)
            {

            }
            finally
            {
                if (writer != null)
                {
                    writer.Dispose();
                }
            }

            return false;
        }

        public static bool TryLoadObject<T>(String filename, out T content)
        {
            return TryLoadObject<T>(filename, TimeSpan.MaxValue, out content);
        }

        public static bool TryLoadObject<T>(String filename, TimeSpan maxAge, out T content)
        {
            content = default(T);
            TextReader reader = null;
            
            try
            {
                IsolatedStorageFile isoStorage = IsolatedStorageFile.GetUserStoreForApplication();

                if (isoStorage.FileExists(filename))
                {
                    DateTime lastWriteTime = isoStorage.GetLastWriteTime(filename).LocalDateTime;
                    if ((DateTime.Now - lastWriteTime) > maxAge)
                    {
                        return false;
                    }

                    IsolatedStorageFileStream file = isoStorage.OpenFile(filename, FileMode.OpenOrCreate);
                    reader = new StreamReader(file);
                    XmlSerializer xs = new XmlSerializer(typeof(T));

                    content = (T)xs.Deserialize(reader);
                    reader.Close();

                    return true;
                }
            }
            catch (Exception e)
            {

            }
            finally
            {
                if (reader != null)
                {
                    reader.Dispose();
                }
            }

            return false;
        }
    }
}
