// Authentication Manager - Version 2.0 - Fixed isLoggedIn validation
class AuthManager {
    constructor() {
        this.token = this.getCookie('userToken') || localStorage.getItem('authToken');
        this.currentUser = JSON.parse(localStorage.getItem('currentUser') || 'null');
        
        // 在初始化时验证认证信息的有效性
        this.validateAuthData();
    }
    
    // 验证认证数据的有效性
    validateAuthData() {
        if (this.token && this.currentUser) {
            // 检查是否是测试数据或无效数据
            if (this.currentUser.password === '' || 
                (this.currentUser.phone && this.currentUser.phone.includes('1111111111'))) {
                console.log('Invalid auth data detected during initialization, clearing...');
                this.clearAuth();
                this.token = null;
                this.currentUser = null;
            }
        }
    }

    // Cookie helper methods
    setCookie(name, value, days = 7) {
        const expires = new Date();
        expires.setTime(expires.getTime() + (days * 24 * 60 * 60 * 1000));
        document.cookie = `${name}=${value};expires=${expires.toUTCString()};path=/;SameSite=Lax`;
    }

    getCookie(name) {
        const nameEQ = name + "=";
        const ca = document.cookie.split(';');
        for (let i = 0; i < ca.length; i++) {
            let c = ca[i];
            while (c.charAt(0) === ' ') c = c.substring(1, c.length);
            if (c.indexOf(nameEQ) === 0) return c.substring(nameEQ.length, c.length);
        }
        return null;
    }

    deleteCookie(name) {
        document.cookie = `${name}=;expires=Thu, 01 Jan 1970 00:00:00 UTC;path=/;`;
    }

    // Check if logged in
    isLoggedIn() {
        const token = this.getCookie('userToken') || localStorage.getItem('authToken');
        const user = JSON.parse(localStorage.getItem('currentUser') || 'null');
        
        // 基本检查：token 和 user 必须存在
        if (!token || !user) {
            return false;
        }
        
        // 验证 token 格式（应该是有效的字符串，不能为空或只包含空格）
        if (typeof token !== 'string' || token.trim().length === 0) {
            console.log('Invalid token format, clearing auth');
            this.clearAuth();
            return false;
        }
        
        // 验证用户信息的完整性
        if (!user.id || !user.phone) {
            console.log('Invalid user data, clearing auth');
            this.clearAuth();
            return false;
        }
        
        // 检查是否是测试数据或无效数据
        if (user.password === '' || user.phone.includes('1111111111')) {
            console.log('Test or invalid user data detected, clearing auth');
            this.clearAuth();
            return false;
        }
        
        return true;
    }

    // Check if admin
    isAdmin() {
        return this.currentUser && this.currentUser.isAdmin === true;
    }

    // Set auth info
    setAuth(token, user) {
        this.token = token;
        this.currentUser = user;
        
        // Store in both Cookie and localStorage
        this.setCookie('userToken', token, 7); // 7 days expiry
        localStorage.setItem('authToken', token);
        localStorage.setItem('currentUser', JSON.stringify(user));
        localStorage.setItem('isLoggedIn', 'true');
    }

    // Clear auth info
    clearAuth() {
        this.token = null;
        this.currentUser = null;
        
        // Clear Cookie and localStorage
        this.deleteCookie('userToken');
        localStorage.removeItem('authToken');
        localStorage.removeItem('currentUser');
        localStorage.removeItem('isLoggedIn');
        localStorage.removeItem('userToken'); // Legacy compatibility
    }

    // Get current user
    getCurrentUser() {
        return this.currentUser;
    }

    // Get auth token
    getToken() {
        return this.token;
    }

    // Check page access permission
    checkPageAccess() {
        const currentPage = window.location.pathname.split('/').pop() || 'index.html';
        const protectedPages = ['home.html', 'account.html', 'withdraw.html', 'recharge.html', 'orders.html', 'service.html', 'invite.html', 'start.html', 'order.html', 'profile.html'];
        
        // If protected page and user not logged in, redirect to login
        if (protectedPages.includes(currentPage) && !this.isLoggedIn()) {
            console.log('Unauthenticated user accessing protected page, redirecting to login');
            window.location.replace('login.html');
            return false;
        }
        
        // If logged in but on login/register page, redirect to home (avoid circular redirect)
        if (this.isLoggedIn() && (currentPage === 'login.html' || currentPage === 'register.html')) {
            console.log('Logged in user accessing login/register page, redirecting to home');
            // Use replace to avoid leaving record in browser history
            window.location.replace('home.html');
            return false;
        }
        
        return true;
    }

    // Logout
    logout() {
        this.clearAuth();
        
        // Clear admin related info
        localStorage.removeItem('adminToken');
        localStorage.removeItem('isAdminLoggedIn');
        this.deleteCookie('adminToken');
        
        // Show logout success message
        if (typeof showAlert === 'function') {
            showAlert('logout-alert', 'Logout successful', 'success');
            setTimeout(() => {
                window.location.href = 'login.html';
            }, 1000);
        } else {
            window.location.href = 'login.html';
        }
    }
}

// Create global auth manager instance
const authManager = new AuthManager();

// Initialize auth state on page load
document.addEventListener('DOMContentLoaded', function() {
    // 首先验证当前的认证状态
    const isLoggedIn = authManager.isLoggedIn();
    console.log('Auth manager initialized, current login status:', isLoggedIn);
    
    // 如果验证失败，确保清除所有认证信息
    if (!isLoggedIn) {
        console.log('Authentication validation failed, clearing all auth data');
        authManager.clearAuth();
    }
    
    authManager.checkPageAccess();
});

// Export for other scripts
window.authManager = authManager;