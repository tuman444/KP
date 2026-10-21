using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Excel = Microsoft.Office.Interop.Excel;
using Word = Microsoft.Office.Interop.Word;

namespace OnlineCinema.Helpers
{
    /// <summary>
    /// Выгрузка отчётов в Word и Excel через COM-интерфейсы Microsoft Office (Interop).
    /// Нужны ссылки на проект: COM -> Microsoft Word / Excel Object Library,
    /// а на компьютере должен быть установлен Microsoft Office.
    /// </summary>
    public static class ReportExporter
    {
        // Файлы сохраняем в "Документы" пользователя, а не в D:\ (там может не быть такого диска).
        private static string BuildPath(string prefix, string extension)
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(folder, $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}");
        }

        // ============================== WORD ==============================
        public static string ExportToWord()
        {
            var reports = StatsService.BuildUserReports();
            if (reports.Count == 0)
                throw new InvalidOperationException("Нет данных для отчёта.");

            var app = new Word.Application();
            Word.Document doc = null;
            try
            {
                doc = app.Documents.Add();

                for (int i = 0; i < reports.Count; i++)
                {
                    var rep = reports[i];

                    // 1. Заголовок: ФИО пользователя (встроенный стиль не зависит от языка Word)
                    Word.Paragraph titlePar = doc.Paragraphs.Add();
                    Word.Range titleRange = titlePar.Range;
                    titleRange.Text = rep.FullName;
                    titlePar.set_Style(Word.WdBuiltinStyle.wdStyleHeading1);
                    titleRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    titleRange.InsertParagraphAfter();

                    // 2. Таблица "Жанр | Просмотрено фильмов"
                    Word.Paragraph tablePar = doc.Paragraphs.Add();
                    Word.Range tableRange = tablePar.Range;
                    Word.Table table = doc.Tables.Add(tableRange, rep.GenreViews.Count + 1, 2);
                    table.Borders.InsideLineStyle = table.Borders.OutsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                    table.Range.Cells.VerticalAlignment = Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;

                    table.Cell(1, 1).Range.Text = "Жанр";
                    table.Cell(1, 2).Range.Text = "Просмотрено фильмов";
                    table.Rows[1].Range.Font.Bold = 1;
                    table.Rows[1].Shading.BackgroundPatternColor = Word.WdColor.wdColorGray15;

                    for (int j = 0; j < rep.GenreViews.Count; j++)
                    {
                        table.Cell(j + 2, 1).Range.Text = rep.GenreViews[j].Key;
                        table.Cell(j + 2, 2).Range.Text = rep.GenreViews[j].Value.ToString();
                    }

                    // 3. Самая высокая и самая низкая оценки пользователя
                    Word.Paragraph bestPar = doc.Paragraphs.Add();
                    Word.Range bestRange = bestPar.Range;
                    if (rep.HasRatings)
                    {
                        bestRange.Text = $"Самая высокая оценка: «{rep.BestTitle}» — {rep.BestRating}/10";
                        bestRange.Font.Color = Word.WdColor.wdColorDarkGreen;
                        bestRange.InsertParagraphAfter();

                        Word.Paragraph worstPar = doc.Paragraphs.Add();
                        Word.Range worstRange = worstPar.Range;
                        worstRange.Text = $"Самая низкая оценка: «{rep.WorstTitle}» — {rep.WorstRating}/10";
                        worstRange.Font.Color = Word.WdColor.wdColorDarkRed;
                        worstRange.InsertParagraphAfter();
                    }
                    else
                    {
                        bestRange.Text = "Оценок пока нет";
                        bestRange.Font.Color = Word.WdColor.wdColorGray50;
                        bestRange.InsertParagraphAfter();
                    }

                    // 4. Разрыв страницы между пользователями (после последнего не нужен)
                    if (i < reports.Count - 1)
                        doc.Words.Last.InsertBreak(Word.WdBreakType.wdPageBreak);
                }

                // Верхний колонтитул: дата. Нижний: номер страницы.
                foreach (Word.Section section in doc.Sections)
                {
                    Word.Range header = section.Headers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary].Range;
                    header.Text = "Отчёт от " + DateTime.Now.ToString("dd.MM.yyyy");
                    header.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphRight;

                    Word.Range footer = section.Footers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary].Range;
                    footer.Fields.Add(footer, Word.WdFieldType.wdFieldPage);
                    footer.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                }

                // Сохраняем в .docx и .pdf
                string docxPath = BuildPath("Отчет_по_пользователям", "docx");
                string pdfPath = Path.ChangeExtension(docxPath, ".pdf");
                doc.SaveAs2(docxPath);
                doc.ExportAsFixedFormat(pdfPath, Word.WdExportFormat.wdExportFormatPDF);

                app.Visible = true;     // показываем готовый документ
                return docxPath;
            }
            catch
            {
                // при ошибке не оставляем в памяти "зависший" Word
                try { if (doc != null) doc.Close(false); } catch { }
                try { app.Quit(false); } catch { }
                throw;
            }
        }

        // ============================== EXCEL ==============================
        public static string ExportToExcel()
        {
            var reports = StatsService.BuildUserReports();
            if (reports.Count == 0)
                throw new InvalidOperationException("Нет данных для отчёта.");

            var app = new Excel.Application();
            app.SheetsInNewWorkbook = 1;                 // в новой книге ровно один лист
            Excel.Workbook wb = null;
            try
            {
                wb = app.Workbooks.Add();
                var usedNames = new HashSet<string>();

                for (int i = 0; i < reports.Count; i++)
                {
                    var rep = reports[i];

                    // Первый лист уже есть, остальные добавляем в конец
                    Excel.Worksheet ws = i == 0
                        ? (Excel.Worksheet)wb.Worksheets[1]
                        : (Excel.Worksheet)wb.Worksheets.Add(After: wb.Worksheets[wb.Worksheets.Count]);
                    ws.Name = SafeSheetName(rep.FullName, usedNames);

                    // Заголовок
                    Set(ws, "A1", "Пользователь: " + rep.FullName);
                    ws.Range["A1"].Font.Bold = true;
                    ws.Range["A1"].Font.Size = 14;

                    // Шапка таблицы
                    Set(ws, "A3", "Жанр");
                    Set(ws, "B3", "Просмотрено фильмов");
                    Excel.Range header = ws.Range["A3", "B3"];
                    header.Font.Bold = true;
                    header.Interior.Color = 0xD9D9D9;

                    // Данные
                    int row = 4;
                    foreach (var gv in rep.GenreViews)
                    {
                        Set(ws, "A" + row, gv.Key);
                        Set(ws, "B" + row, gv.Value);
                        row++;
                    }

                    // Итоговая строка с ФОРМУЛОЙ (в .Formula функции пишутся по-английски: SUM)
                    Set(ws, "A" + row, "Итого");
                    ws.Range["B" + row].Formula = "=SUM(B4:B" + (row - 1) + ")";
                    ws.Range["A" + row, "B" + row].Font.Bold = true;

                    // Границы и ширина столбцов таблицы
                    Excel.Range tableRange = ws.Range["A3", "B" + row];
                    tableRange.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
                    tableRange.Columns.AutoFit();

                    // Оценки под таблицей
                    row += 2;
                    if (rep.HasRatings)
                    {
                        Set(ws, "A" + row, $"Самая высокая оценка: «{rep.BestTitle}» — {rep.BestRating}/10");
                        row++;
                        Set(ws, "A" + row, $"Самая низкая оценка: «{rep.WorstTitle}» — {rep.WorstRating}/10");
                    }
                    else
                    {
                        Set(ws, "A" + row, "Оценок пока нет");
                    }
                }

                string path = BuildPath("Отчет_по_пользователям", "xlsx");
                wb.SaveAs(path);
                app.Visible = true;
                return path;
            }
            catch
            {
                try { if (wb != null) wb.Close(false); } catch { }
                try { app.Quit(); } catch { }
                throw;
            }
        }

        /// <summary>Записать значение в ячейку по адресу ("A1", "B7"...).</summary>
        private static void Set(Excel.Worksheet ws, string address, object value)
        {
            ws.Range[address].Value2 = value;
        }

        /// <summary>Имя листа Excel: до 31 символа, без \ / * ? : [ ], без повторов.</summary>
        private static string SafeSheetName(string name, HashSet<string> used)
        {
            var bad = new[] { '\\', '/', '*', '?', ':', '[', ']' };
            string clean = new string(name.Where(ch => !bad.Contains(ch)).ToArray()).Trim();
            if (clean.Length == 0) clean = "Лист";
            if (clean.Length > 28) clean = clean.Substring(0, 28);

            string result = clean;
            int n = 2;
            while (!used.Add(result))
                result = clean + " " + n++;
            return result;
        }
    }
}
