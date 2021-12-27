﻿using FlowChart.Database.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace FlowChart.Database.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly SQLiteAsyncConnection db;

        public ReadingMonth CurrentMonth { get; private set; }

        public DatabaseService()
        {
            string databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FlowChartData.db");
            db = new SQLiteAsyncConnection(databasePath);

            MainThread.BeginInvokeOnMainThread(async () => await InitializeAsync());
        }

        public async Task InitializeAsync() 
        {
            await db.CreateTableAsync<ReadingMonth>();
            await db.CreateTableAsync<Reading>();

            List<ReadingMonth> months = await GetMonthsAsync();
            if (months.Count == 0)
                CurrentMonth = await InsertNewMonth();
            else
            {
                CurrentMonth = months.FirstOrDefault(month => month.Month == DateTime.UtcNow.Month && month.Year == DateTime.UtcNow.Year);
                if (CurrentMonth == null)
                    CurrentMonth = await InsertNewMonth();
            }
        }

        public async Task<List<ReadingMonth>> GetMonthsAsync()
        {
            AsyncTableQuery<ReadingMonth> query = db.Table<ReadingMonth>();
            List<ReadingMonth> months = await query.ToListAsync();
            return months;
        }

        public async Task<List<Reading>> GetMonthAsync(int monthId)
        {
            try
            {
                AsyncTableQuery<Reading> query = db.Table<Reading>().Where(reading => reading.MonthId == monthId);
                List<Reading> readings = await query.ToListAsync();
                return readings;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex);
                return null;
            }
        }

        public async Task<List<Reading>> GetMonthAsync(DateTime date)
        {
            AsyncTableQuery<ReadingMonth> query = db.Table<ReadingMonth>().Where(month => month.Month == date.Month && month.Year == date.Year);
            ReadingMonth readingMonth = await query.FirstAsync();
            return await GetMonthAsync(readingMonth.Id);
        }

        public async Task<int> InsertReadingAsync(Reading reading)
        {
            reading.MonthId = CurrentMonth.Id;
            await db.InsertAsync(reading);

            CurrentMonth.ReadingCount++;
            await db.UpdateAsync(CurrentMonth);

            return reading.Id;
        }

        public async Task<bool> UpdateReadingAsync(Reading reading)
        {
            try
            {
                await db.UpdateAsync(reading);
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteReadingAsync(Reading reading)
        {
            try 
            {
                await db.DeleteAsync(reading);
                return true;
            }
            catch { return false; }
        }

        public async Task ClearDatabaseAsync() 
        {
            await db.DeleteAllAsync<Reading>();
            await db.DeleteAllAsync<ReadingMonth>();
        }

        private async Task<ReadingMonth> InsertNewMonth() 
        {
            ReadingMonth month = new ReadingMonth()
            {
                Month = DateTime.UtcNow.Month,
                Year = DateTime.UtcNow.Year
            };

            await db.InsertAsync(month);
            return month;
        }
    }
}
