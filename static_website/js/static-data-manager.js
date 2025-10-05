/**
 * 静态数据管理器 - 用于管理所有JSON数据文件
 * 替代原有的API调用，直接操作本地JSON文件
 */
class StaticDataManager {
    constructor() {
        this.dataCache = {};
        this.baseUrl = './data/';
    }

    /**
     * 加载JSON数据文件
     */
    async loadData(filename) {
        if (this.dataCache[filename]) {
            return this.dataCache[filename];
        }

        try {
            const response = await fetch(`${this.baseUrl}${filename}`);
            if (!response.ok) {
                throw new Error(`Failed to load ${filename}`);
            }
            const data = await response.json();
            this.dataCache[filename] = data;
            return data;
        } catch (error) {
            console.error(`Error loading ${filename}:`, error);
            return [];
        }
    }

    /**
     * 保存数据到localStorage（模拟数据持久化）
     */
    saveData(filename, data) {
        this.dataCache[filename] = data;
        localStorage.setItem(`static_data_${filename}`, JSON.stringify(data));
    }

    /**
     * 从localStorage恢复数据
     */
    restoreData(filename) {
        const saved = localStorage.getItem(`static_data_${filename}`);
        if (saved) {
            try {
                this.dataCache[filename] = JSON.parse(saved);
                return this.dataCache[filename];
            } catch (error) {
                console.error(`Error parsing saved data for ${filename}:`, error);
            }
        }
        return null;
    }

    /**
     * 获取用户数据
     */
    async getUsers() {
        let users = this.restoreData('users.json');
        if (!users) {
            users = await this.loadData('users.json');
        }
        return users || [];
    }

    /**
     * 获取管理员数据
     */
    async getAdmins() {
        let admins = this.restoreData('super_admins.json');
        if (!admins) {
            admins = await this.loadData('super_admins.json');
        }
        return admins || [];
    }

    /**
     * 获取代理商数据
     */
    async getAgents() {
        let agents = this.restoreData('agents.json');
        if (!agents) {
            agents = await this.loadData('agents.json');
        }
        return agents || [];
    }

    /**
     * 获取订单数据
     */
    async getOrders() {
        let orders = this.restoreData('orders.json');
        if (!orders) {
            orders = await this.loadData('orders.json');
        }
        return orders || [];
    }

    /**
     * 获取VIP等级数据
     */
    async getVipLevels() {
        let vipLevels = this.restoreData('vip_levels.json');
        if (!vipLevels) {
            vipLevels = await this.loadData('vip_levels.json');
        }
        return vipLevels || [];
    }

    /**
     * 获取任务数据
     */
    async getTasks() {
        let tasks = this.restoreData('tasks.json');
        if (!tasks) {
            tasks = await this.loadData('tasks.json');
        }
        return tasks || [];
    }

    /**
     * 获取用户统计数据
     */
    async getUserStats() {
        let stats = this.restoreData('user_stats.json');
        if (!stats) {
            stats = await this.loadData('user_stats.json');
        }
        return stats || [];
    }

    /**
     * 获取余额数据
     */
    async getBalances() {
        let balances = this.restoreData('balances.json');
        if (!balances) {
            balances = await this.loadData('balances.json');
        }
        return balances || [];
    }

    /**
     * 用户登录验证
     */
    async validateUser(username, password) {
        const users = await this.getUsers();
        const user = users.find(u => u.username === username);
        
        if (user) {
            // 简单的密码验证（实际项目中应该使用加密）
            if (user.password === password || this.verifyPassword(password, user.password)) {
                return {
                    success: true,
                    user: {
                        id: user.id,
                        username: user.username,
                        role: user.role || 'user',
                        vipLevel: user.vipLevel || 1
                    }
                };
            }
        }
        
        return { success: false, message: '用户名或密码错误' };
    }

    /**
     * 管理员登录验证
     */
    async validateAdmin(username, password) {
        const admins = await this.getAdmins();
        const admin = admins.find(a => a.username === username);
        
        if (admin) {
            if (admin.password === password || this.verifyPassword(password, admin.password)) {
                return {
                    success: true,
                    user: {
                        id: admin.id,
                        username: admin.username,
                        role: 'admin'
                    }
                };
            }
        }
        
        return { success: false, message: '管理员用户名或密码错误' };
    }

    /**
     * 代理商登录验证
     */
    async validateAgent(username, password) {
        const agents = await this.getAgents();
        const agent = agents.find(a => a.username === username);
        
        if (agent) {
            if (agent.password === password || this.verifyPassword(password, agent.password)) {
                return {
                    success: true,
                    user: {
                        id: agent.id,
                        username: agent.username,
                        role: 'agent'
                    }
                };
            }
        }
        
        return { success: false, message: '代理商用户名或密码错误' };
    }

    /**
     * 简单的密码验证（支持BCrypt格式检查）
     */
    verifyPassword(plainPassword, hashedPassword) {
        // 如果是BCrypt格式的密码，这里只做简单比较
        // 在实际静态网站中，建议使用明文密码或客户端加密
        if (hashedPassword && hashedPassword.startsWith('$2')) {
            // BCrypt格式，暂时返回false，建议更新为明文密码
            return false;
        }
        return plainPassword === hashedPassword;
    }

    /**
     * 添加新用户
     */
    async addUser(userData) {
        const users = await this.getUsers();
        const newUser = {
            id: Date.now().toString(),
            ...userData,
            createdAt: new Date().toISOString()
        };
        users.push(newUser);
        this.saveData('users.json', users);
        return newUser;
    }

    /**
     * 更新用户数据
     */
    async updateUser(userId, updateData) {
        const users = await this.getUsers();
        const userIndex = users.findIndex(u => u.id === userId);
        if (userIndex !== -1) {
            users[userIndex] = { ...users[userIndex], ...updateData };
            this.saveData('users.json', users);
            return users[userIndex];
        }
        return null;
    }

    /**
     * 添加新订单
     */
    async addOrder(orderData) {
        const orders = await this.getOrders();
        const newOrder = {
            id: Date.now().toString(),
            ...orderData,
            createdAt: new Date().toISOString(),
            status: 'pending'
        };
        orders.push(newOrder);
        this.saveData('orders.json', orders);
        return newOrder;
    }

    /**
     * 更新订单状态
     */
    async updateOrder(orderId, updateData) {
        const orders = await this.getOrders();
        const orderIndex = orders.findIndex(o => o.id === orderId);
        if (orderIndex !== -1) {
            orders[orderIndex] = { ...orders[orderIndex], ...updateData };
            this.saveData('orders.json', orders);
            return orders[orderIndex];
        }
        return null;
    }

    /**
     * 获取用户的订单
     */
    async getUserOrders(userId) {
        const orders = await this.getOrders();
        return orders.filter(order => order.userId === userId);
    }

    /**
     * 获取用户余额
     */
    async getUserBalance(userId) {
        const balances = await this.getBalances();
        const userBalance = balances.find(b => b.userId === userId);
        return userBalance ? userBalance.balance : 0;
    }

    /**
     * 更新用户余额
     */
    async updateUserBalance(userId, newBalance) {
        const balances = await this.getBalances();
        const balanceIndex = balances.findIndex(b => b.userId === userId);
        
        if (balanceIndex !== -1) {
            balances[balanceIndex].balance = newBalance;
        } else {
            balances.push({
                userId: userId,
                balance: newBalance,
                updatedAt: new Date().toISOString()
            });
        }
        
        this.saveData('balances.json', balances);
        return newBalance;
    }

    /**
     * 清除所有缓存数据
     */
    clearCache() {
        this.dataCache = {};
        // 清除localStorage中的数据
        Object.keys(localStorage).forEach(key => {
            if (key.startsWith('static_data_')) {
                localStorage.removeItem(key);
            }
        });
    }
}

// 创建全局实例
window.staticDataManager = new StaticDataManager();