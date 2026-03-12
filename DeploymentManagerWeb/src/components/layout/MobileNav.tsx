import * as React from 'react';
import { useLocation } from 'react-router-dom';
import {
    Box,
    Drawer,
    Stack,
    List,
    Divider,
    Typography,
    useTheme,
} from '@mui/material';
import { usePermission } from '../../hooks/usePermission';
import { navItems } from './navConfig';
import { filterNavItems, loadNavGroupsState, saveNavGroupsState } from './navUtils';
import { NavItemRenderer } from './NavItemRenderer';

interface MobileNavProps {
    onClose: () => void;
    open: boolean;
}

export function MobileNav({ onClose, open }: MobileNavProps): React.JSX.Element {
    const location = useLocation();
    const { can } = usePermission();
    const theme = useTheme();
    const isDarkMode = theme.palette.mode === 'dark';

    // Load initial state from localStorage
    const [openGroups, setOpenGroups] = React.useState<Record<string, boolean>>(() =>
        loadNavGroupsState('mobileNavOpenGroups')
    );

    const toggleGroup = (key: string) => {
        setOpenGroups(prev => {
            const newState = {
                ...prev,
                [key]: !prev[key]
            };
            saveNavGroupsState('mobileNavOpenGroups', newState);
            return newState;
        });
    };

    const filteredNavItems = filterNavItems(navItems, can);

    return (
        <Drawer
            anchor="left"
            onClose={onClose}
            open={open}
            PaperProps={{
                sx: {
                    bgcolor: '#274549',
                    color: 'var(--mui-palette-common-white)',
                    width: '320px',
                    display: 'flex',
                    flexDirection: 'column',
                    borderRight: isDarkMode ? '1px solid rgba(139, 154, 247, 0.1)' : 'none',
                },
            }}
            sx={{ display: { lg: 'none' } }}
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
                }}
            >
                <List sx={{ px: 2 }}>
                    {filteredNavItems.map((item) => (
                        <NavItemRenderer
                            key={item.key}
                            item={item}
                            openGroups={openGroups}
                            onToggleGroup={toggleGroup}
                            onItemClick={onClose}
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
        </Drawer>
    );
}