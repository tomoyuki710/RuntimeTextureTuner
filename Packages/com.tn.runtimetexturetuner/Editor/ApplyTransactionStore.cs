using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// Apply の記録をプロジェクトの Library 配下に保存する。Library は通常バージョン管理の対象外なので、共有されない。
    /// </summary>
    public static class ApplyTransactionStore
    {
        /// <summary>
        /// 調整機能が Editor で使う作業フォルダー。
        /// </summary>
        public const string WorkFolder = "Library/RuntimeTextureTuner";

        private const string TransactionFolder = WorkFolder + "/Transactions";

        public static string Save(ApplyTransaction transaction)
        {
            string path = $"{TransactionFolder}/transaction_{transaction.Id}.json";
            SessionSerializer.WriteFile(path, JsonUtility.ToJson(transaction, true));
            return path;
        }

        /// <summary>
        /// 保存されている記録を、新しい順に読み込む。読めないファイルは飛ばす。
        /// </summary>
        public static List<ApplyTransaction> LoadAll()
        {
            var transactions = new List<ApplyTransaction>();
            if (!Directory.Exists(TransactionFolder))
            {
                return transactions;
            }

            string[] files = Directory.GetFiles(TransactionFolder, "transaction_*.json");
            Array.Sort(files, StringComparer.Ordinal);
            Array.Reverse(files);
            foreach (string file in files)
            {
                if (TryLoad(file, out ApplyTransaction transaction))
                {
                    transactions.Add(transaction);
                }
            }

            return transactions;
        }

        private static bool TryLoad(string path, out ApplyTransaction transaction)
        {
            transaction = null;
            try
            {
                transaction = JsonUtility.FromJson<ApplyTransaction>(File.ReadAllText(path));
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }

            return transaction != null && !string.IsNullOrEmpty(transaction.Id);
        }
    }
}
