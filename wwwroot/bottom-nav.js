/**
 * 底部导航栏组件
 * 提供页面导航和当前页面高亮功能
 */

// 导航栏配置
const NAV_CONFIG = [
    { href: 'home.html', icon: 'fas fa-home', text: '首页' },
    { href: 'orders.html', icon: 'fas fa-list-alt', text: '订单' },
    { href: 'start.html', icon: 'fas fa-play-circle', text: '开始' },
    { href: 'service.html', icon: 'fas fa-headset', text: '客服' },
    { href: 'account.html', icon: 'fas fa-user', text: '账户' }
];

/**
 * 创建底部导航栏HTML
 * @param {string} currentPage - 当前页面文件名
 * @returns {string} 导航栏HTML字符串
 */
function createBottomNav(currentPage) {
    const navItems = NAV_CONFIG.map(item => {
        const isActive = item.href === currentPage ? 'active' : '';
        return `
            <a href="${item.href}" class="nav-item ${isActive}">
                <div class="nav-icon"><i class="${item.icon}"></i></div>
                <div class="nav-text">${item.text}</div>
            </a>
        `;
    }).join('');
    
    return `<div class="bottom-nav">${navItems}</div>`;
}

/**
 * 初始化底部导航栏
 * 自动检测当前页面并设置高亮
 */
function initBottomNav() {
    // 获取当前页面文件名
    const currentPage = window.location.pathname.split('/').pop() || 'home.html';
    
    // 创建导航栏HTML
    const navHtml = createBottomNav(currentPage);
    
    // 插入到页面中
    document.body.insertAdjacentHTML('beforeend', navHtml);
}

/**
 * 更新导航栏高亮状态
 * @param {string} activePage - 要高亮的页面文件名
 */
function updateNavActive(activePage) {
    const navItems = document.querySelectorAll('.nav-item');
    navItems.forEach(item => {
        const href = item.getAttribute('href');
        if (href === activePage) {
            item.classList.add('active');
        } else {
            item.classList.remove('active');
        }
    });
}

// 页面加载完成后自动初始化导航栏
document.addEventListener('DOMContentLoaded', function() {
    initBottomNav();
});

// 导出函数供外部使用
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        createBottomNav,
        initBottomNav,
        updateNavActive
    };
}