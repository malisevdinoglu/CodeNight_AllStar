import axios from 'axios'
import { appConfig } from '../app/config'

export const apiClient = axios.create({
  baseURL: appConfig.apiUrl,
  timeout: 3000,
  headers: {
    'Content-Type': 'application/json',
  },
})
