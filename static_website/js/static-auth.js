/**
 * 静态认证管理器 - 用于处理用户登录、会话管理
 * 使用localStorage进行会话持久化
 */
class StaticAuthManager {
    constructor() {
        this.currentUser = null;
        this.sessionKey = 'static_user_session';
        this.init();
    }

    /**
     * 初始化认证管理器
     */
    init() {
        this.loadSession();
    }

    /**
     * 用户登录
     */
    async login(username, password, userType = 'user') {
        try {
            let result;
            
            switch (userType) {
                case 'admin':
                    result = await window.staticDataManager.validateAdmin(username, password);
                    break;
                case 'agent':
                    result = await window.staticDataManager.validateAgent(username, password);
                    break;
                default:
                    result = await window.staticDataManager.validateUser(username, password);
            }

            if (result.success) {
                this.currentUser = result.user;
                this.saveSession();
                return { success: true, user: result.user };
            } else {
                return { success: false, message: result.message };
            }
        } catch (error) {
            console.error('Login error:', error);
            return { success: false, message: '登录过程中发生错误' };
        }
    }

    /**
     * 用户登出
     */
    logout() {
        this.currentUser = null;
        this.clearSession();
    }

    /**
     * 检查用户是否已登录
     */
    isLoggedIn() {
        return this.currentUser !== null;
    }

    /**
     * 获取当前用户信息
     */
    getCurrentUser() {
        return this.currentUser;
    }

    /**
     * 检查用户权限
     */
    hasPermission(requiredRole) {
        if (!this.currentUser) return false;
        
        const roleHierarchy = {
            'user': 1,
            'agent': 2,
            'admin': 3
        };
        
        const userLevel = roleHierarchy[this.currentUser.role] || 0;
        const requiredLevel = roleHierarchy[requiredRole] || 0;
        
        return userLevel >= requiredLevel;
    }

    /**
     * 保存会话到localStorage
     */
    saveSession() {
        if (this.currentUser) {
            const sessionData = {
                user: this.currentUser,
                timestamp: Date.now(),
                expires: Date.now() + (24 * 60 * 60 * 1000) // 24小时过期
            };
            localStorage.setItem(this.sessionKey, JSON.stringify(sessionData));
        }
    }

    /**
     * 从localStorage加载会话
     */
    loadSession() {
        try {
            const sessionData = localStorage.getItem(this.sessionKey);
            if (sessionData) {
                const parsed = JSON.parse(sessionData);
                
                // 检查会话是否过期
                if (parsed.expires && Date.now() < parsed.expires) {
                    this.currentUser = parsed.user;
                } else {
                    this.clearSession();
                }
            }
        } catch (error) {
            console.error('Error loading session:', error);
            this.clearSession();
        }
    }

    /**
     * 清除会话
     */
    clearSession() {
        localStorage.removeItem(this.sessionKey);
        this.currentUser = null;
    }

    /**
     * 刷新会话（延长过期时间）
     */
    refreshSession() {
        if (this.currentUser) {
            this.saveSession();
        }
    }

    /**
     * 获取用户角色显示名称
     */
    getRoleDisplayName(role) {
        const roleNames = {
            'user': '普通用户',
            'agent': '代理商',
            'admin': '管理员'
        };
        return roleNames[role] || '未知角色';
    }

    /**
     * 检查页面访问权限
     */
    checkPageAccess(requiredRole = null) {
        if (requiredRole && !this.hasPermission(requiredRole)) {
            this.redirectToLogin();
            return false;
        }
        return true;
    }

    /**
     * 重定向到登录页面
     */
    redirectToLogin() {
        const currentPage = window.location.pathname;
        const loginUrl = currentPage.includes('admin') ? 'admin-login.html' : 'login.html';
        window.location.href = loginUrl;
    }

    /**
     * 重定向到主页
     */
    redirectToHome() {
        if (this.currentUser) {
            switch (this.currentUser.role) {
                case 'admin':
                    window.location.href = 'admin.html';
                    break;
                case 'agent':
                    window.location.href = 'agent.html';
                    break;
                default:
                    window.location.href = 'home.html';
            }
        } else {
            window.location.href = 'login.html';
        }
    }
}

/**
 * 页面权限保护装饰器
 */
function requireAuth(requiredRole = null) {
    return function(target, propertyKey, descriptor) {
        const originalMethod = descriptor.value;
        
        descriptor.value = function(...args) {
            if (!window.staticAuth.checkPageAccess(requiredRole)) {
                return;
            }
            return originalMethod.apply(this, args);
        };
        
        return descriptor;
    };
}

/**
 * 页面初始化时的认证检查
 */
function initPageAuth(requiredRole = null) {
    document.addEventListener('DOMContentLoaded', function() {
        if (!window.staticAuth.checkPageAccess(requiredRole)) {
            return;
        }
        
        // 显示用户信息
        const user = window.staticAuth.getCurrentUser();
        if (user) {
            updateUserDisplay(user);
        }
    });
}

/**
 * 更新页面上的用户显示信息
 */
function updateUserDisplay(user) {
    // 更新用户名显示
    const usernameElements = document.querySelectorAll('.username-display');
    usernameElements.forEach(el => {
        el.textContent = user.username;
    });
    
    // 更新用户角色显示
    const roleElements = document.querySelectorAll('.user-role-display');
    roleElements.forEach(el => {
        el.textContent = window.staticAuth.getRoleDisplayName(user.role);
    });
    
    // 更新VIP等级显示
    if (user.vipLevel) {
        const vipElements = document.querySelectorAll('.vip-level-display');
        vipElements.forEach(el => {
            el.textContent = `VIP${user.vipLevel}`;
        });
    }
}

/**
 * 通用登录表单处理
 */
function setupLoginForm(formId, userType = 'user') {
    const form = document.getElementById(formId);
    if (!form) return;
    
    form.addEventListener('submit', async function(e) {
        e.preventDefault();
        
        const username = form.querySelector('input[name="username"]').value;
        const password = form.querySelector('input[name="password"]').value;
        const submitBtn = form.querySelector('button[type="submit"]');
        const errorDiv = form.querySelector('.error-message');
        
        // 显示加载状态
        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.textContent = '登录中...';
        }
        
        try {
            const result = await window.staticAuth.login(username, password, userType);
            
            if (result.success) {
                // 登录成功，重定向到相应页面
                window.staticAuth.redirectToHome();
            } else {
                // 显示错误信息
                if (errorDiv) {
                    errorDiv.textContent = result.message;
                    errorDiv.style.display = 'block';
                }
            }
        } catch (error) {
            console.error('Login form error:', error);
            if (errorDiv) {
                errorDiv.textContent = '登录过程中发生错误，请重试';
                errorDiv.style.display = 'block';
            }
        } finally {
            // 恢复按钮状态
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.textContent = '登录';
            }
        }
    });
}

/**
 * 设置登出按钮
 */
function setupLogoutButton(buttonSelector = '.logout-btn') {
    document.addEventListener('click', function(e) {
        if (e.target.matches(buttonSelector)) {
            e.preventDefault();
            window.staticAuth.logout();
            window.location.href = 'login.html';
        }
    });
}

// 创建全局实例
window.staticAuth = new StaticAuthManager();

// 自动设置登出按钮
document.addEventListener('DOMContentLoaded', function() {
    setupLogoutButton();
});