import * as React from 'react';
import {
    Box,
    Stack,
    List,
    Divider,
    Typography,
    useTheme,
} from '@mui/material';
import { useSidebar } from '../../contexts/SidebarContext';
import { usePermission } from '../../hooks/usePermission';
import { navItems } from './navConfig';
import { filterNavItems, loadNavGroupsState, saveNavGroupsState } from './navUtils.ts';
import { NavItemRenderer } from './NavItemRenderer';

export function SideNav(): React.JSX.Element {
    const { isCollapsed } = useSidebar();
    const { can } = usePermission();
    const theme = useTheme();
    const isDarkMode = theme.palette.mode === 'dark';

    // Load initial state from localStorage
    const [openGroups, setOpenGroups] = React.useState<Record<string, boolean>>(() =>
        loadNavGroupsState('sideNavOpenGroups')
    );

    const toggleGroup = (key: string) => {
        setOpenGroups(prev => {
            const newState = {
                ...prev,
                [key]: !prev[key]
            };
            saveNavGroupsState('sideNavOpenGroups', newState);
            return newState;
        });
    };

    const filteredNavItems = filterNavItems(navItems, can);

    return (
        <Box
            sx={{
                bgcolor: '#274549',
                color: 'var(--mui-palette-common-white)',
                display: { xs: 'none', lg: 'flex' },
                flexDirection: 'column',
                height: '100vh',
                left: 0,
                maxWidth: '100%',
                position: 'fixed',
                top: 0,
                width: '280px',
                zIndex: 1100,
                transform: isCollapsed ? 'translateX(-100%)' : 'translateX(0)',
                transition: 'all 0.3s ease',
                boxShadow: isDarkMode
                    ? '4px 0 24px rgba(0, 0, 0, 0.5)'
                    : '4px 0 24px rgba(0, 0, 0, 0.12)',
                borderRight: isDarkMode ? '1px solid rgba(139, 154, 247, 0.1)' : 'none',
            }}
        >
            {/* Logo Section */}
            <Stack spacing={1} sx={{ p: 3, pb: 2 }}>
                <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', mb: 1 }}>
                    <img src="/images/TheGrandHoTram.png" alt="Logo" style={{ height: 70, width: 'auto' }} />
                </Box>
                <Typography
                    variant="h6"
                    sx={{
                        textAlign: 'center',
                        fontWeight: 700,
                        fontSize: '1.1rem',
                        letterSpacing: 0.5,
                        color: 'white',
                        textShadow: '0 2px 4px rgba(0,0,0,0.2)',
                    }}
                >
                    Deployment Manager
                </Typography>
            </Stack>

            <Divider sx={{ borderColor: 'rgba(255, 255, 255, 0.15)', mx: 2 }} />

            {/* Navigation Items */}
            <Box
                sx={{
                    flex: '1 1 auto',
                    overflow: 'auto',
                    py: 2,
                    '&::-webkit-scrollbar': {
                        width: '6px',
                    },
                    '&::-webkit-scrollbar-track': {
                        background: 'rgba(255, 255, 255, 0.05)',
                    },
                    '&::-webkit-scrollbar-thumb': {
                        background: 'rgba(255, 255, 255, 0.2)',
                        borderRadius: '3px',
                    },
                }}
            >
                <List sx={{ px: 2 }}>
                    {filteredNavItems.map((item) => (
                        <NavItemRenderer
                            key={item.key}
                            item={item}
                            openGroups={openGroups}
                            onToggleGroup={toggleGroup}
                        />
                    ))}
                </List>
            </Box>

            {/* Theme Toggle at Bottom */}
            {/* <Box sx={{ p: 2, mt: 'auto' }}>
                <Divider sx={{ borderColor: 'rgba(255, 255, 255, 0.15)', mb: 2 }} />
                <Box sx={{ display: 'flex', justifyContent: 'center' }}>
                    <ThemeToggleButton />
                </Box>
            </Box> */}
        </Box>
    );
}