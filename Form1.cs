using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using Lucene.Net.Analysis.Ru;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Version = Lucene.Net.Util.Version;
using org.apache.pdfbox.util;
using org.apache.pdfbox.pdmodel;
using System.Drawing;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            progressBar1.Hide();
            listBox1.Items.Clear();
            string target = textBox2.Text; 
            IEnumerable<DataSample> result = Lucene.Search(target, "Content");
            if (result.Count()!=0)
                foreach (DataSample v in result)
                {
                    listBox1.Items.AddRange(new string[] { v.Name });
                }
            else
                listBox1.Items.Add("Поиск не дал результатов. Попробуйте поменять запрос.");
            listBox1.IntegralHeight = true;
            listBox1.HorizontalScrollbar = true;
            Graphics g = listBox1.CreateGraphics();
            int hzSize = ((int)g.MeasureString(listBox1.Items[0].ToString(), listBox1.Font).Width+100)*2;
            listBox1.HorizontalExtent = hzSize;
        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox2.Show();
            label2.Show();
            button1.Enabled = false;
            listBox1.IntegralHeight = true;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (textBox2.Text.Length < 3 ||(textBox2.Text.Length <= 3 && textBox2.Text.Contains(' ')))
                 button1.Hide();
            else button1.Show();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
        }

        private void button2_Click(object sender, EventArgs e)
        {
            progressBar1.Show();
            progressBar1.Value = 0;
            button2.Hide();
            label3.Text = "Подготовка к индексации файлов";
            Application.DoEvents();
            
            progressBar1.Step = 10;
            DateTime dold = DateTime.Now;
            progressBar1.PerformStep();
            //Lucene.ClearIndex();
            progressBar1.PerformStep();
            DirectoryInfo source = new DirectoryInfo("C:/Users/УРП/Downloads/");// C:/Users/УРП/Downloads/";
            label3.Text = "Проиндексировано 0 файлов";
            Application.DoEvents();
            
            try
            {
                FileInfo[] files = source.GetFiles("*.pdf", SearchOption.AllDirectories);
                int D;  //количество документов
                D = files.Count();
                try
                {
                    progressBar1.Step=5;
                    Application.DoEvents();

                    if (D != 0)
                    {
                        int indexed_docs = 0;
                        double inc = 70 / D;
                        foreach(var f in files)
                        {
                            try
                            {
                                Lucene.AddUpdateIndex(new DataSample { Score = 1, Size = f.Length, LastModified = f.LastWriteTime, Name = f.FullName, Content = "" });
                            }
                            catch (UnauthorizedAccessException uaex)
                            {
                                MessageBox.Show("Отказано в доступе к данному файлу "+f.FullName+". " + uaex.Message+ " \n Нажмите ОК чтобы продолжить.", "Сообщение об ошибке");
                                label3.Text = "Не удалось проиндексировать файл " + f.FullName;
                                Application.DoEvents();
                            }
                            indexed_docs++;
                            while (inc >= 5) { progressBar1.PerformStep(); inc -= 5;  }
                            inc += 50 / D;
                            label3.Text = "Проиндексировано " + indexed_docs + " файла(ов) из " + D+". (/"+f.Name +")";
                            Application.DoEvents();

                        }
                        label3.Text = "Перестройка индексного файла.";
                        Application.DoEvents();

                        Lucene.Optimize();
                        progressBar1.PerformStep();
                        TimeSpan sp = DateTime.Now - dold;
                        MessageBox.Show(sp.ToString() + " время индексации", "Индексация завершена успешно.");
                        label3.Text = "Индексация завершена";
                    }
                    else
                    {
                        MessageBox.Show("В данной директории отсутствуют Pdf-файлы. Измените настройки и обновите приложение.", "Сообщение об ошибке");
                    }
                    Application.DoEvents();

                    progressBar1.Step =20;
                    Application.DoEvents();
                    progressBar1.PerformStep();
                    progressBar1.PerformStep();
                }
                catch 
                {
                    MessageBox.Show("В данной директории отсутствуют Pdf-файлы.", "Сообщение об ошибке");
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                MessageBox.Show("Директория не найдена. Измените настройки и обновите приложение. " + ex.Message,"Сообщение об ошибке");
            }
            catch when (source == new DirectoryInfo(""))
            {
                MessageBox.Show("Задан пустой адрес. Измените путь к директории. Измените настройки и обновите приложение.", "Сообщение об ошибке");
            }
            textBox2.Show();
            button2.Show();
            label2.Show();
            button1.Enabled = true;
        }
    }
    public class DataSample
    {
        public string Name { get; set; }
        public double Score { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public string Content { get; set; }
    }
    public class pdfTextExtractor
    {
        public static String PDFText(String PDFFilePath) 
        {

            PDDocument doc = PDDocument.load(PDFFilePath);
            PDFTextStripper stripper = new PDFTextStripper();
            var text = " ";
            try
            {
                text=stripper.getText(doc);
                return text;
            }
            catch (UnauthorizedAccessException e)
            {
                MessageBox.Show("Невозможно скопировать текст из Пдф "+PDFFilePath+". "+e.Message, "Сообщение об ошибке");
                return "";
            }
            catch(FileLoadException FLe)
            {
                MessageBox.Show("Невозможно загрузить Пдф " + PDFFilePath + ". " + FLe.Message, "Сообщение об ошибке");
                return "";
            }
            catch when(text=="")
            {
                MessageBox.Show("Невозможно загрузить Пдф " + PDFFilePath + ". ", "Сообщение об ошибке");
                return "";
            }
            finally
            {
                doc.close();
            }
        }
    }
    public class Lucene
    {
        private static IEnumerable<DataSample> _search(string searchQuery, string searchField = "")
        {
            if(searchQuery != "" && searchQuery != " ")
            {
                using (var searcher = new IndexSearcher(_directory))//, false,))
                {
                    var hits_limit = 10;
                    var query = fparseQuery(searchQuery);
                    var hits = searcher.Search(query, null, hits_limit, Sort.RELEVANCE).ScoreDocs;
                    var results = _mapDocumentToDataList(hits, searcher);
                    searcher.Dispose();
                    return results;
                }
            }
            else
            {
                MessageBox.Show("Неправильно введен поисковый запрос", "Сообщение об ошибке");
                return null;
            }
        }
        private static DataSample _mapDocumentToData(Document doc)
        {
            return new DataSample
            {
                Name = doc.Get("Name"),
                Score = Convert.ToDouble(doc.Get("Score")),
                Size = Convert.ToInt64(doc.Get("Size")),
                LastModified = Convert.ToDateTime(doc.Get("LastModified")),
                Content = doc.Get("Content")
            };
        }
        private static IEnumerable<DataSample> _mapDocumentToDataList(IEnumerable<ScoreDoc> hits, IndexSearcher searcher)
        {
            return hits.Select(hit => _mapDocumentToData(searcher.Doc(hit.Doc))).ToList();
        }
        private static BooleanQuery fparseQuery(string searchQuery)
        {
            BooleanQuery boolQuery = new BooleanQuery();
            string[] terms = searchQuery.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);//empty occur
            if (terms.Length == 1)
            {
                TermQuery termQuery = new TermQuery(new Term("Content", terms[0]));
                boolQuery.Add(termQuery, Occur.MUST);
            }
            else if (terms.Length == 2)
            {
                PhraseQuery phraseQuery = new PhraseQuery() { Slop = 1, Boost = 3 };
                TermQuery termQuery = new TermQuery(new Term("Content", terms[0]));
                boolQuery.Add(termQuery, Occur.SHOULD);
                phraseQuery.Add(new Term("Content", terms[0]));
                termQuery = new TermQuery(new Term("Content", terms[1]));
                boolQuery.Add(termQuery, Occur.SHOULD);
                phraseQuery.Add(new Term("Content", terms[1]));
                boolQuery.Add(phraseQuery, Occur.SHOULD);
            }
            else if (terms.Length == 3)
            {
                PhraseQuery phraseQuery = new PhraseQuery() { Slop = 3 };
                for (int i = 0; i < terms.Length; i++)
                {
                    TermQuery termQuery = new TermQuery(new Term("Content", terms[i]));
                    boolQuery.Add(termQuery, Occur.SHOULD);
                    phraseQuery.Add(new Term("Content", terms[i]));
                }
                boolQuery.Add(phraseQuery, Occur.SHOULD);
            }
            else
            {
                PhraseQuery phraseQuery = new PhraseQuery() { Slop = terms.Length / 2, Boost = 3 };
                PhraseQuery fphraseQuery = new PhraseQuery() { Slop = terms.Length / 4, Boost = 2 };
                PhraseQuery lphraseQuery = new PhraseQuery() { Slop = terms.Length / 4, Boost = 2 };

                for (int i = 0; i < terms.Length; i++)
                {
                    TermQuery termQuery = new TermQuery(new Term("Content", terms[i]));
                    boolQuery.Add(termQuery, Occur.SHOULD);
                    phraseQuery.Add(new Term("Content", terms[i]));
                    if (i < terms.Length / 2) { fphraseQuery.Add(new Term("Content", terms[i])); }
                    else { lphraseQuery.Add(new Term("Content", terms[i])); }
                }
                boolQuery.Add(phraseQuery, Occur.SHOULD);
                boolQuery.Add(lphraseQuery, Occur.SHOULD);
                boolQuery.Add(fphraseQuery, Occur.SHOULD);
            }


            return boolQuery;
        }
        public static IEnumerable<DataSample> Search(string input, string fieldName = "")
        {
            var rs = new RussianStemmer();
            char[] ch = { ' ', '.', '\t', ',', '_', '\\', '/', '|', '"', '\'', '{', '}', '[', ']', '<', '>', ':', ';', '!', '&', '?' };
            var terms = input.Trim().Replace("-", " ").ToLower().Split(ch, StringSplitOptions.RemoveEmptyEntries);//
            input = "";
            for (int i = 0; i < terms.Length; i++)
            {
                if (terms[i].Length >= 3)
                {
                    input += rs.Stem(terms[i]); input += " ";//"* "
                }
            }

            return _search(input, fieldName);
        }
        public static IEnumerable<DataSample> SearchDefault(string input, string fieldName = "")
        {
            return string.IsNullOrEmpty(input) ? new List<DataSample>() : _search(input, fieldName);
        }

        private static string _luceneDir = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "lucene_index");
        private static FSDirectory _directoryTemp;
        private static FSDirectory _directory
        {
            get
            {
                if (_directoryTemp == null) _directoryTemp = FSDirectory.Open(new DirectoryInfo(_luceneDir));
                if (IndexWriter.IsLocked(_directoryTemp)) IndexWriter.Unlock(_directoryTemp);
                var lockFilePath = Path.Combine(_luceneDir, "write.lock");
                if (File.Exists(lockFilePath)) File.Delete(lockFilePath);
                return _directoryTemp;
            }
        }
        private static void _addToIndex(DataSample dataSample, RussianAnalyzer analyzer)
        {
            var searcher = new IndexSearcher(_directory,true);
            var searchQuery = new TermQuery(new Term("Name", dataSample.Name.ToString()));
            var hits = searcher.Search(searchQuery, 1).ScoreDocs;
            var results = _mapDocumentToDataList(hits, searcher);
            searcher.Dispose();
            if (results.Count()!=0)
            {
                TimeSpan ts = results.ElementAt(0).LastModified - dataSample.LastModified;
                if (Math.Abs(ts.TotalSeconds)<=2)
                {
                    if (results.ElementAt(0).Size == dataSample.Size)//одно имя, дата изменения, размер файла-> не меняем
                    {
                        ;
                    }
                    else //одно имя, дата изменения, отличается размер файла-> апдейт
                    {
                        var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
                        var doc = new Document();
                        doc.Add(new Field("Score", dataSample.Score.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                        doc.Add(new Field("Size", dataSample.Size.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                        doc.Add(new Field("LastModified", dataSample.LastModified.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                        doc.Add(new Field("Name", dataSample.Name, Field.Store.YES, Field.Index.NOT_ANALYZED));
                        doc.Add(new Field("Content", pdfTextExtractor.PDFText(dataSample.Name), Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.YES));
                        writer.UpdateDocument(new Term("Name",dataSample.Name),doc);
                        writer.Dispose();
                    }
                }
                else //одно имя,  отличается  дата изменения-> апдейт
                {
                    var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
                    var doc = new Document();
                    doc.Add(new Field("Score", dataSample.Score.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                    doc.Add(new Field("Size", dataSample.Size.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                    doc.Add(new Field("LastModified", dataSample.LastModified.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                    doc.Add(new Field("Name", dataSample.Name, Field.Store.YES, Field.Index.NOT_ANALYZED));
                    doc.Add(new Field("Content", pdfTextExtractor.PDFText(dataSample.Name), Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.YES));
                    writer.UpdateDocument(new Term("Name", dataSample.Name), doc);
                    writer.Dispose();
                }
            }
            else//такого имени нет в индексе -> добавить
            {
                var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
                var doc = new Document();
                doc.Add(new Field("Score", dataSample.Score.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                doc.Add(new Field("Size", dataSample.Size.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                doc.Add(new Field("LastModified", dataSample.LastModified.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                doc.Add(new Field("Name", dataSample.Name, Field.Store.YES, Field.Index.NOT_ANALYZED));
                doc.Add(new Field("Content", pdfTextExtractor.PDFText(dataSample.Name), Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.YES));
                writer.AddDocument(doc);
                writer.Dispose();
            }
        }
        public static void AddUpdateIndex(IEnumerable<DataSample> dataSamples)
        {
            var analyzer = new RussianAnalyzer(Version.LUCENE_30);
            foreach (var dataSample in dataSamples) _addToIndex(dataSample, analyzer);
            analyzer.Close();
        }
        public static void AddUpdateIndex(DataSample dataSample)
        {
            AddUpdateIndex(new List<DataSample> { dataSample });
        }
        public static bool ClearIndex()
        {
            try
            {
                var analyzer = new RussianAnalyzer(Version.LUCENE_30);
                using (var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    writer.DeleteAll();
                    analyzer.Close();
                    writer.Dispose();
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        public static void Optimize()
        {
            var analyzer = new RussianAnalyzer(Version.LUCENE_30);
            using (var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                analyzer.Close();
                writer.Optimize();
                writer.Dispose();
            }
        }
    }
}