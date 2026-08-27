import {
  Dialog, DialogContent, DialogTitle, IconButton, List, ListItemButton, ListItemText,
  MenuItem, Stack, TextField, Typography
} from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import { useMemo, useState } from 'react';
import { useCategoryGroups } from '@/modules/category-groups/hooks';
import type { CategoryListItem } from '@/types';

interface Props {
  open: boolean;
  onClose: () => void;
  categories: CategoryListItem[];
  onSelect: (category: CategoryListItem) => void;
}

const UNGROUPED_LABEL = 'Sans groupe';

export function CategoryPickerDialog({ open, onClose, categories, onSelect }: Props) {
  const groups = useCategoryGroups();
  const [search, setSearch] = useState('');
  const [groupFilter, setGroupFilter] = useState('');

  const groupOptions = useMemo(() => {
    const present = new Set(categories.map((c) => c.groupName ?? UNGROUPED_LABEL));
    return (groups.data ?? [])
      .map((g) => g.name)
      .filter((name) => present.has(name))
      .concat(present.has(UNGROUPED_LABEL) ? [UNGROUPED_LABEL] : []);
  }, [groups.data, categories]);

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    return categories.filter((c) => {
      const groupLabel = c.groupName ?? UNGROUPED_LABEL;
      if (groupFilter && groupLabel !== groupFilter) return false;
      if (!term) return true;
      return c.label.toLowerCase().includes(term) || c.code.toLowerCase().includes(term);
    });
  }, [categories, search, groupFilter]);

  const grouped = useMemo(() => {
    const map = new Map<string, CategoryListItem[]>();
    for (const c of filtered) {
      const key = c.groupName ?? UNGROUPED_LABEL;
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(c);
    }
    return Array.from(map.entries()).sort(([a], [b]) => a.localeCompare(b));
  }, [filtered]);

  const handleClose = () => {
    setSearch('');
    setGroupFilter('');
    onClose();
  };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ pr: 6 }}>
        Choisir une catégorie
        <IconButton onClick={handleClose} sx={{ position: 'absolute', right: 8, top: 8 }}>
          <CloseIcon fontSize="small" />
        </IconButton>
      </DialogTitle>
      <DialogContent dividers sx={{ p: 0 }}>
        <Stack direction="row" spacing={1.5} sx={{ p: 2 }}>
          <TextField
            fullWidth size="small" autoFocus
            label="Rechercher"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <TextField
            select size="small" label="Groupe" value={groupFilter}
            onChange={(e) => setGroupFilter(e.target.value)}
            sx={{ minWidth: 180 }}
          >
            <MenuItem value="">Tous les groupes</MenuItem>
            {groupOptions.map((g) => <MenuItem key={g} value={g}>{g}</MenuItem>)}
          </TextField>
        </Stack>

        {filtered.length === 0 ? (
          <Typography variant="body2" color="text.secondary" sx={{ p: 3, textAlign: 'center' }}>
            Aucune catégorie ne correspond à la recherche.
          </Typography>
        ) : (
          <List dense sx={{ maxHeight: 420, overflowY: 'auto', py: 0 }}>
            {grouped.map(([groupLabel, items]) => [
              <ListItemText
                key={`h-${groupLabel}`}
                sx={{ px: 2, pt: 1.5, pb: 0.5 }}
                primary={
                  <Typography variant="caption" color="text.secondary" fontWeight={600} textTransform="uppercase">
                    {groupLabel}
                  </Typography>
                }
              />,
              ...items.map((c) => (
                <ListItemButton key={c.id} dense onClick={() => { onSelect(c); handleClose(); }}>
                  <ListItemText primary={`${c.code} / ${c.label}`} />
                </ListItemButton>
              ))
            ])}
          </List>
        )}
      </DialogContent>
    </Dialog>
  );
}
