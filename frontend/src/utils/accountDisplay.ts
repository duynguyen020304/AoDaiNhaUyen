interface AccountIdentity {
  fullName: string;
  email?: string | null;
  roles?: readonly string[];
}

export function isAdminAccount(user: AccountIdentity): boolean {
  return user.roles?.some((role) => role.toLowerCase() === 'admin') ?? false;
}

export function formatAccountDisplayName(user: AccountIdentity): string {
  const fullName = user.fullName.trim();
  if (!fullName || fullName.toLowerCase() === 'admin') {
    return isAdminAccount(user) || fullName.toLowerCase() === 'admin' ? 'Quản trị viên' : 'Tài khoản';
  }

  return fullName || user.email?.split('@')[0] || 'Tài khoản';
}

export function getAccountInitial(user: AccountIdentity): string {
  return formatAccountDisplayName(user).charAt(0).toUpperCase();
}

export function getAccountRoleLabel(user: AccountIdentity): string {
  return isAdminAccount(user) ? 'Quản trị viên Hà Uyên' : 'Khách hàng Hà Uyên';
}
