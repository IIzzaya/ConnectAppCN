using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SQLite4Unity3d;
using UnityEngine;

namespace ConnectApp.Utils {
    public class SQLiteDBManager {
        const string DBName = "messenger";
        
        static SQLiteDBManager m_Instance;

        public static SQLiteDBManager instance {
            get {
                if (m_Instance == null) {
                    m_Instance = new SQLiteDBManager();
                }

                return m_Instance;
            }
        }
        
        SQLiteConnection m_Connection;

        SQLiteDBManager() {
            this.Reset();
        }

        void Reset(bool force = false) {
            if (!force && this.m_Connection != null) {
                return;
            }
            
            this.m_Connection?.Close();

#if UNITY_EDITOR
            var dbPath = $"Assets/{DBName}.db";
#else
            var dbPath = $"{Application.persistentDataPath}/{DBName}.db";
#endif
            try {
                bool dbExists = File.Exists(dbPath);
                this.m_Connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
                
                if (!dbExists) {
                    this.InitDB();
                }
            }
            catch (Exception e) {
                Debug.Log($"fatal error: fail to connect to database: {DBName}, error msg = {e.Message}");
            }
        }


        public void ClearAll() {
            this.Reset();
            this.InitDB();
        }

        void InitDB() {
            this.m_Connection.DropTable<DBMessageLite>();
            this.m_Connection.CreateTable<DBMessageLite>();
            this.m_Connection.DropTable<FileRecordLite>();
            this.m_Connection.CreateTable<FileRecordLite>();
        }


        public string GetCachedFilePath(string url) {
            var ret = this.m_Connection.Table<FileRecordLite>().Where(record => record.url == url);

            if (!ret.Any()) {
                return null;
            }

            if (ret.Count() == 1) {
                return ret.First().filepath;
            }

            Debug.Assert(false, "fatal error: duplicated files are mapping to one url.");
            return null;
        }

        public void UpdateCachedFilePath(string url, string filePath) {
            this.m_Connection.Insert(new FileRecordLite {url = url, filepath = filePath}, extra: "OR REPLACE");
        }
        

        public void SaveMessages(List<DBMessageLite> data) {
            this.m_Connection.InsertAll(data, extra: "OR REPLACE");
        }

        public IEnumerable<DBMessageLite> QueryMessages(string channelId, long maxNonce = -1, int maxCount = 5) {
            if (maxNonce == -1) {
                return this.m_Connection.Table<DBMessageLite>().Where(message => message.channelId == channelId)
                    .OrderByDescending(message => message.nonce).Take(maxCount);
            }
            else {
                return this.m_Connection.Table<DBMessageLite>().Where(message => message.channelId == channelId &&
                                                                                 message.nonce < maxNonce)
                    .OrderByDescending(message => message.nonce).Take(maxCount);
            }
        }
    }
}