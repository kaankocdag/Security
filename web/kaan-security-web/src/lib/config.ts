export const config = {
  // IPv6 localhost (::1) sorunlarını önlemek için 127.0.0.1 tercih edilir
  apiBaseUrl: process.env.KAAN_API_BASE_URL || 'http://127.0.0.1:5089',
  publicApiBase: '/api/backend',
  cookieNames: {
    access: 'ksp_at',
    refresh: 'ksp_rt',
    user: 'ksp_user'
  }
};
