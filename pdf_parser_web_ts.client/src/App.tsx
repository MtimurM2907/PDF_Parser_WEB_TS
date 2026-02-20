import { useState } from 'react';
import './App.css';

interface ParsedDocument {
  id: number;
  fileName: string;
  uploadedAt: string;
  fullText: string;
  structuredJson: string;
  aiSummary?: string | null;
}

function App() {
  const [file, setFile] = useState<File | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<ParsedDocument | null>(null);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setResult(null);
    setError(null);
    const f = e.target.files?.[0] ?? null;
    setFile(f);
  };

  const handleUpload = async () => {
    if (!file) {
      setError('Выберите PDF файл.');
      return;
    }

    setIsUploading(true);
    setError(null);
    try {
      const formData = new FormData();
      formData.append('file', file);

      const response = await fetch('/api/pdf/parse', {
        method: 'POST',
        body: formData,
      });

      if (!response.ok) {
        const text = await response.text();
        throw new Error(text || 'Ошибка при загрузке файла');
      }

      const data = (await response.json()) as ParsedDocument;
      setResult(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Неизвестная ошибка');
    } finally {
      setIsUploading(false);
    }
  };

  const handleDownloadJson = () => {
    if (!result) return;
    const blob = new Blob([result.structuredJson], { type: 'application/json;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${result.id}_${result.fileName}.json`;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="app-root">
      <h1>PDF Parser</h1>
      <p>Загрузите PDF, чтобы извлечь текст и получить JSON.</p>

      <div className="upload-panel">
        <label className="file-input">
          <span className="file-input-label">{file ? file.name : 'Выберите PDF-файл'}</span>
          <span className="file-input-button">Обзор</span>
          <input type="file" accept="application/pdf" onChange={handleFileChange} />
        </label>
        <button onClick={handleUpload} disabled={isUploading || !file}>
          {isUploading ? 'Обработка...' : 'Загрузить и распарсить'}
        </button>
      </div>

      {error && <div className="error">{error}</div>}

      {result && (
        <div className="result">
          <h2>Результат</h2>
          <p>
            <strong>Файл:</strong> {result.fileName}
          </p>
          <p>
            <strong>Загружен:</strong> {new Date(result.uploadedAt).toLocaleString()}
          </p>

          <div className="result-actions">
            <button onClick={handleDownloadJson}>Скачать JSON</button>
          </div>

          <div className="columns">
            <div className="column">
              <h3>Текст</h3>
              <pre className="text-output">{result.fullText || '(текст не распознан)'}</pre>
            </div>
            <div className="column">
              <h3>JSON</h3>
              <pre className="json-output">{result.structuredJson}</pre>
            </div>
          </div>

          <div className="columns">
            <div className="column">
              <h3>AI‑описание документа</h3>
              <pre className="text-output">
                {result.aiSummary && result.aiSummary.trim().length > 0
                  ? result.aiSummary
                  : '(описание от GigaChat отсутствует)'}
              </pre>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default App;