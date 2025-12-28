export type LoginRequest = {
  userName: string;
  password: string;
}

export type LoginResponse = {
  userName: string;
  token: string;
  refreshToken: string;
  roles: string[];
  employeeId: number;
  employeeCode: string;
  tokenExpiration: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}