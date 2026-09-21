// LocalStorage Keys
const TOKEN_KEY = 'apex_jwt_token';
const USER_KEY = 'apex_user_data';

// Helper to save auth state after login/register
function setAuthState(token, userData) {
    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(USER_KEY, JSON.stringify(userData));
}

// Helper to retrieve token
function getToken() {
    return localStorage.getItem(TOKEN_KEY);
}

// Helper to retrieve logged-in user info
function getUserData() {
    const data = localStorage.getItem(USER_KEY);
    return data ? JSON.parse(data) : null;
}

// Logout function
function logout() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    window.location.href = '/Account/Login';
}

// Wrapper around fetch API to automatically inject JWT Bearer token
async function authFetch(url, options = {}) {
    const token = getToken();

    if (!options.headers) {
        options.headers = {};
    }

    if (token) {
        options.headers['Authorization'] = `Bearer ${token}`;
    }

    if (options.body && typeof options.body === 'object' && !(options.body instanceof FormData)) {
        options.headers['Content-Type'] = 'application/json';
        options.body = JSON.stringify(options.body);
    }

    const response = await fetch(url, options);

    // If token is expired or unauthorized, clear storage and redirect
    if (response.status === 401) {
        logout();
    }

    return response;
}

async function checkAuth(){
    const res = await fetch("/Admin/CheckAuth");
    return res;
}