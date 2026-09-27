-- 部屋（room）ごとに一体のレイドボス。HPだけ共有する。
-- Supabase の SQL Editor でこのファイルを実行する。

create table if not exists public.raid_bosses (
  room text primary key,
  week int not null default 1,
  floor int not null default 1,
  clears int not null default 0,
  skin int not null default 0,
  skin_path text not null default '',
  enemy_name text not null default '',
  max_hp int not null default 1,
  hp int not null default 1,
  participants int not null default 0,
  generation int not null default 1,
  updated_at timestamptz not null default now()
);

create table if not exists public.raid_members (
  room text not null,
  generation int not null,
  code text not null,
  name text not null default '',
  damage int not null default 0,
  look_csv text not null default '',
  primary key (room, generation, code)
);

create table if not exists public.raid_presents (
  id text primary key,
  code text not null,
  room text not null,
  generation int not null,
  week int not null default 1,
  floor int not null default 1,
  item_id text not null default '',
  item_name text not null default '',
  item_kind text not null default '',
  diamonds int not null default 50,
  claimed int not null default 0
);

alter table public.raid_bosses enable row level security;
alter table public.raid_members enable row level security;
alter table public.raid_presents enable row level security;

drop policy if exists "raid_bosses_read" on public.raid_bosses;
drop policy if exists "raid_bosses_insert" on public.raid_bosses;
drop policy if exists "raid_bosses_update" on public.raid_bosses;
create policy "raid_bosses_read" on public.raid_bosses for select using (true);
create policy "raid_bosses_insert" on public.raid_bosses for insert with check (true);
create policy "raid_bosses_update" on public.raid_bosses for update using (true);

drop policy if exists "raid_members_read" on public.raid_members;
drop policy if exists "raid_members_insert" on public.raid_members;
drop policy if exists "raid_members_update" on public.raid_members;
create policy "raid_members_read" on public.raid_members for select using (true);
create policy "raid_members_insert" on public.raid_members for insert with check (true);
create policy "raid_members_update" on public.raid_members for update using (true);

drop policy if exists "raid_presents_read" on public.raid_presents;
drop policy if exists "raid_presents_insert" on public.raid_presents;
drop policy if exists "raid_presents_update" on public.raid_presents;
create policy "raid_presents_read" on public.raid_presents for select using (true);
create policy "raid_presents_insert" on public.raid_presents for insert with check (true);
create policy "raid_presents_update" on public.raid_presents for update using (true);

grant select, insert, update on public.raid_bosses to anon, authenticated;
grant select, insert, update on public.raid_members to anon, authenticated;
grant select, insert, update on public.raid_presents to anon, authenticated;
