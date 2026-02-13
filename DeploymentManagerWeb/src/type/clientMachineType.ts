export interface ClientMachineResponse {
    id: string;
    machineId: string;
    machineName: string;
    computerName: string;
    userName: string;
    domainName: string;
    ipAddress: string;
    macAddress: string;
    osVersion: string;
    osArchitecture: string;
    status: string;
    lastHeartbeat: string;
    registeredAt: string;
    installedApplications: string[];
    clientVersion: string;
    location: string;
    pendingTasksCount: number;
}

