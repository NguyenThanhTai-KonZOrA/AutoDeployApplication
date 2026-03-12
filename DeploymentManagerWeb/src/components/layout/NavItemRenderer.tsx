import * as React from 'react';
import { useLocation, Link as RouterLink } from 'react-router-dom';
import {
    Box,
    List,
    ListItem,
    ListItemButton,
    ListItemIcon,
    ListItemText,
    Collapse,
} from '@mui/material';
import {
    ExpandMore as ExpandMoreIcon,
    ExpandLess as ExpandLessIcon,
} from '@mui/icons-material';
import type { NavItem } from '../../type/commonType';

interface NavItemRendererProps {
    item: NavItem;
    depth?: number;
    openGroups: Record<string, boolean>;
    onToggleGroup: (key: string) => void;
    onItemClick?: () => void;
}

export const NavItemRenderer: React.FC<NavItemRendererProps> = ({
    item,
    depth = 0,
    openGroups,
    onToggleGroup,
    onItemClick,
}) => {
    const location = useLocation();
    const Icon = item.icon;

    // If item has children, render as a group
    if (item.children && item.children.length > 0) {
        const isOpen = openGroups[item.key];

        return (
            <React.Fragment key={item.key}>
                <ListItem disablePadding sx={{ mb: 0.5 }}>
                    <ListItemButton
                        onClick={() => onToggleGroup(item.key)}
                        sx={{
                            borderRadius: 2,
                            color: 'rgba(255, 255, 255, 0.85)',
                            backgroundColor: 'transparent',
                            transition: 'all 0.2s ease',
                            '&:hover': {
                                backgroundColor: 'rgba(255, 255, 255, 0.08)',
                                color: 'white',
                                '& .expand-icon': {
                                    opacity: 1,
                                }
                            },
                            py: 1.2,
                            px: 2,
                        }}
                    >
                        <ListItemIcon
                            sx={{
                                color: 'inherit',
                                minWidth: 40,
                            }}
                        >
                            <Icon fontSize="small" />
                        </ListItemIcon>
                        <ListItemText
                            primary={item.title}
                            primaryTypographyProps={{
                                fontSize: '0.9rem',
                                fontWeight: 500,
                            }}
                        />
                        <Box
                            className="expand-icon"
                            sx={{
                                display: 'flex',
                                alignItems: 'center',
                                opacity: isOpen ? 1 : 0.5,
                                transition: 'opacity 0.2s ease'
                            }}
                        >
                            {isOpen ? <ExpandLessIcon fontSize="small" /> : <ExpandMoreIcon fontSize="small" />}
                        </Box>
                    </ListItemButton>
                </ListItem>
                <Collapse in={isOpen} timeout="auto" unmountOnExit>
                    <List component="div" disablePadding>
                        {item.children.map((child) => (
                            <NavItemRenderer
                                key={child.key}
                                item={child}
                                depth={depth + 1}
                                openGroups={openGroups}
                                onToggleGroup={onToggleGroup}
                                onItemClick={onItemClick}
                            />
                        ))}
                    </List>
                </Collapse>
            </React.Fragment>
        );
    }

    // Render as a single item
    const isActive = location.pathname === item.href;
    const paddingLeft = depth > 0 ? 4 + (depth * 2) : 2;

    return (
        <ListItem key={item.key} disablePadding sx={{ mb: depth > 0 ? 0.5 : 1 }}>
            <ListItemButton
                component={RouterLink}
                to={item.href || '#'}
                onClick={onItemClick}
                sx={{
                    borderRadius: 2,
                    color: isActive ? 'white' : 'rgba(255, 255, 255, 0.85)',
                    backgroundColor: isActive ? 'rgba(255, 255, 255, 0.15)' : 'transparent',
                    backdropFilter: isActive ? 'blur(10px)' : 'none',
                    boxShadow: isActive ? '0 4px 12px rgba(0, 0, 0, 0.15)' : 'none',
                    transition: 'all 0.2s ease',
                    '&:hover': {
                        backgroundColor: isActive
                            ? 'rgba(255, 255, 255, 0.2)'
                            : 'rgba(255, 255, 255, 0.08)',
                        transform: depth === 0 ? 'translateX(4px)' : 'none',
                    },
                    py: 1.2,
                    px: 2,
                    pl: paddingLeft,
                }}
            >
                <ListItemIcon
                    sx={{
                        color: 'inherit',
                        minWidth: 40,
                    }}
                >
                    <Icon fontSize="small" />
                </ListItemIcon>
                <ListItemText
                    primary={item.title}
                    primaryTypographyProps={{
                        fontSize: depth > 0 ? '0.85rem' : '0.9rem',
                        fontWeight: isActive ? 600 : 500,
                    }}
                />
            </ListItemButton>
        </ListItem>
    );
};
