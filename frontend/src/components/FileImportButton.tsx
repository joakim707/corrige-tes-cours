import { useRef, useState } from 'react'
import { api } from '../api/client'
import { extractErrorMessage } from '../api/errors'

interface ExtractedTextResponse {
  fileName: string
  text: string
  characterCount: number
}

interface Props {
  onExtracted: (text: string) => void
  onError: (message: string) => void
}

const ACCEPTED = '.pdf,.docx,.pptx,.md,.txt'

export function FileImportButton({ onExtracted, onError }: Props) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [loading, setLoading] = useState(false)

  async function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    e.target.value = ''
    if (!file) return

    setLoading(true)
    try {
      const formData = new FormData()
      formData.append('file', file)
      const res = await api.post<ExtractedTextResponse>('/api/documents/extract-text', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      onExtracted(res.data.text)
    } catch (err) {
      onError(extractErrorMessage(err, "Impossible d'extraire le texte de ce fichier."))
    } finally {
      setLoading(false)
    }
  }

  return (
    <>
      <input ref={inputRef} type="file" accept={ACCEPTED} hidden onChange={(e) => void handleChange(e)} />
      <button type="button" className="secondary" disabled={loading} onClick={() => inputRef.current?.click()}>
        {loading ? 'Extraction…' : '📎 Importer un fichier'}
      </button>
    </>
  )
}
