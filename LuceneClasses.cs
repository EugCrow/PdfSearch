using System;

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
            text = stripper.getText(doc);
            return text;
        }
        catch (UnauthorizedAccessException e)
        {
            MessageBox.Show("Невозможно скопировать текст из Пдф " + PDFFilePath + ". " + e.Message, "Сообщение об ошибке");
            return "";
        }
        catch (FileLoadException FLe)
        {
            MessageBox.Show("Невозможно загрузить Пдф " + PDFFilePath + ". " + FLe.Message, "Сообщение об ошибке");
            return "";
        }
        catch when (text == "")
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
public class LuceneDoc
{
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
    public static FSDirectory Dir = _directory();
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
    public static IEnumerable<DataSample> _mapDocumentToDataList(IEnumerable<ScoreDoc> hits, IndexSearcher searcher)
    {
        return hits.Select(hit => _mapDocumentToData(searcher.Doc(hit.Doc))).ToList();
    }
}
public class LuceneIndex : LuceneDoc
{
    private static void _addToIndex(DataSample dataSample, RussianAnalyzer analyzer)
    {
        var searcher = new IndexSearcher(Dir, true);
        var searchQuery = new TermQuery(new Term("Name", dataSample.Name.ToString()));
        var hits = searcher.Search(searchQuery, 1).ScoreDocs;
        var results = _mapDocumentToDataList(hits, searcher);
        searcher.Dispose();
        if (results.Count() != 0)
        {
            TimeSpan ts = results.ElementAt(0).LastModified - dataSample.LastModified;
            if (Math.Abs(ts.TotalSeconds) <= 2)
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
                    writer.UpdateDocument(new Term("Name", dataSample.Name), doc);
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
    public static void ClearIndexRecord(string name)
    {
        var analyzer = new RussianAnalyzer(Version.LUCENE_30);
        using (var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
        {
            var searchQuery = new TermQuery(new Term("Name", name.ToString()));
            writer.DeleteDocuments(searchQuery);
            analyzer.Close();
            writer.Dispose();
        }
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
public class LuceneSearch : LuceneDoc
{
    private static IEnumerable<DataSample> _search(string searchQuery, string searchField = "")
    {
        if (searchQuery != "" && searchQuery != " ")
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
}
public class Parser
{
    public static BooleanQuery fparseQuery(string searchQuery)
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
}