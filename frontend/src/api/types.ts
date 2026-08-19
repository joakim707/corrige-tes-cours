export type NiveauScolaire = 'College' | 'Lycee' | 'Superieur'

export interface User {
  id: string
  email: string
  pseudo: string
  level: NiveauScolaire
  createdAt: string
}

export interface AuthResponse {
  accessToken: string
  expiresInSeconds: number
  user: User
}

export interface RegisterRequest {
  email: string
  password: string
  pseudo: string
  level: NiveauScolaire
}

export interface LoginRequest {
  email: string
  password: string
}

export interface Matiere {
  id: string
  nom: string
  couleur: string
  niveau: NiveauScolaire
  createdAt: string
}

export interface CreateMatiereRequest {
  nom: string
  couleur: string
  niveau: NiveauScolaire
}

export interface DashboardStats {
  matieresCount: number
  fichesCount: number
  quizCount: number
  scoreMoyen: number | null
}

export interface Correction {
  id: string
  contenuInput: string
  contenuIA: string
  matiereId: string | null
  createdAt: string
}

export interface SubmitCorrectionRequest {
  exercice: string
  matiereId?: string | null
  demanderCorrectionComplete?: boolean
}

export interface Fiche {
  id: string
  titre: string
  resume: string
  pointsCles: string[]
  definitions: Record<string, string>
  formules: string[]
  matiereId: string | null
  createdAt: string
}

export interface FicheSummary {
  id: string
  titre: string
  resume: string
  matiereId: string | null
  createdAt: string
}

export interface GenerateFicheRequest {
  cours: string
  matiereId?: string | null
}

export type QuestionType = 'Qcm' | 'VraiFaux' | 'Ouverte'

export interface QuizQuestionPlay {
  index: number
  enonce: string
  type: QuestionType
  options: string[]
}

export interface QuizPlay {
  id: string
  titre: string
  questions: QuizQuestionPlay[]
}

export interface QuizSummary {
  id: string
  titre: string
  nombreQuestions: number
  ficheId: string | null
  matiereId: string | null
  createdAt: string
}

export interface GenerateQuizRequest {
  ficheId?: string | null
  sujet?: string | null
  matiereId?: string | null
  nombreQuestions?: number
}

export interface QuizAnswerDetail {
  questionIndex: number
  reponseUtilisateur: string
  correcte: boolean
  explication: string
}

export interface QuizResult {
  id: string
  quizId: string
  score: number
  details: QuizAnswerDetail[]
  passedAt: string
}

export interface QuizResultSummary {
  id: string
  quizId: string
  quizTitre: string
  score: number
  passedAt: string
}
